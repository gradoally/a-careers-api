using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SomeDAO.Backend.Data;
using SomeDAO.Backend.Services;
using Swashbuckle.AspNetCore.Annotations;
using TonLibDotNet;

namespace SomeDAO.Backend.Api
{
    [ApiController]
    [Consumes(System.Net.Mime.MediaTypeNames.Application.Json)]
    [Produces(System.Net.Mime.MediaTypeNames.Application.Json)]
    [Route("/api/[action]")]
    [SwaggerResponse(200, "Request is accepted, processed and response contains requested data.")]
    public class SearchController : ControllerBase
    {
        private readonly ILogger logger;
        private readonly CachedData cachedData;
        private readonly BackendConfig backendConfig;
        private readonly IDbProvider dbProvider;

        public SearchController(ILogger<SearchController> logger, CachedData searchService, IOptions<BackendOptions> backendOptions, IOptions<TonOptions> tonOptions, IDbProvider dbProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.cachedData = searchService ?? throw new ArgumentNullException(nameof(searchService));
            this.backendConfig = new BackendConfig
            {
                MasterContractAddress = backendOptions.Value.MasterAddress,
                Mainnet = tonOptions.Value.UseMainnet,
            };
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
        }

        /// <summary>
        /// Returns general configuration data.
        /// </summary>
        [HttpGet]
        public ActionResult<BackendConfig> Config()
        {
            backendConfig.Categories = cachedData.AllCategories;
            backendConfig.Languages = cachedData.AllLanguages;
            return backendConfig;
        }

        /// <summary>
        /// Returns list of ACTIVE (available to work at) Orders that meet filter.
        /// </summary>
        /// <param name="query">Free query</param>
        /// <param name="category">Show only specified category.</param>
        /// <param name="language">Show only specified language.</param>
        /// <param name="minPrice">Minimum price to include</param>
        /// <param name="orderBy">Sort field: 'createdAt' or 'deadline'.</param>
        /// <param name="sort">Sort order: 'asc' or 'desc'.</param>
        /// <param name="translateTo">Language (key or code/name) of language to translate to. Must match one of supported languages (from config).</param>
        /// <param name="page">Page number to return (default 0).</param>
        /// <param name="pageSize">Page size (default 10, max 100).</param>
        /// <remarks>
        /// <para>
        /// With non-empty <b><paramref name="translateTo"/></b> param returned top-level objects (Orders) will have fields <b>nameTranslated</b>, <b>descriptionTranslated</b> and <b>technicalTaskTranslated</b> filled with translated values of their corresponding original field values.
        /// </para>
        /// <para>
        /// These fields may be null if corresponding value is not translated yet.
        /// Also, these fields will be null if original order language is equal to the language to translate to.
        /// </para>
        /// <para>
        /// Expected usage: <code>… = (item.nameTranslated ?? item.name)</code>.
        /// </para>
        /// </remarks>
        [SwaggerResponse(400, "Invalid request.")]
        [HttpGet]
        public ActionResult<List<Order>> Search(
            string? query,
            string? category,
            string? language,
            decimal? minPrice,
            string orderBy = "createdAt",
            string sort = "asc",
            string? translateTo = null,
            int page = 0,
            int pageSize = 10)
        {

            var orderByMode = orderBy.ToLowerInvariant() switch
            {
                "createdat" => 1,
                "deadline" => 2,
                _ => 0,
            };

            if (orderByMode == 0)
            {
                ModelState.AddModelError(nameof(orderBy), "Invalid value. Use 'createdAt' or 'deadline'.");
            }

            var sortMode = sort.ToLowerInvariant() switch
            {
                "asc" => 1,
                "desc" => 2,
                _ => 0,
            };

            if (sortMode == 0)
            {
                ModelState.AddModelError(nameof(sort), "Invalid value. Use 'asc' or 'desc'.");
            }

            var source = cachedData.ActiveOrders;
            if (!string.IsNullOrEmpty(translateTo)
                && !cachedData.ActiveOrdersTranslated.TryGetValue(translateTo, out source))
            {
                ModelState.AddModelError(nameof(translateTo), "Unknown (unsupported) language value");
            }

            if (page < 0)
            {
                ModelState.AddModelError(nameof(page), "Must be non-negative.");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                ModelState.AddModelError(nameof(pageSize), "Must be between 1 and 100.");
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem();
            }

            var list = SearchActiveOrders(source!, query, category, language, minPrice);

            var ordered = (orderByMode, sortMode) switch
            {
                (1, 1) => list.OrderBy(x => x.CreatedAt),
                (1, _) => list.OrderBy(x => x.Deadline),
                (_, 1) => list.OrderByDescending(x => x.CreatedAt),
                (_, _) => list.OrderByDescending(x => x.Deadline),
            };

            return ordered.Skip(page * pageSize).Take(pageSize).ToList();
        }

        /// <summary>
        /// Returns number of ACTIVE (available to work at) Orders that meet filter.
        /// </summary>
        /// <param name="query">Free query</param>
        /// <param name="category">Show only specified category.</param>
        /// <param name="language">Show only specified language.</param>
        /// <param name="minPrice">Minimum price to include</param>
        [HttpGet]
        public ActionResult<int> SearchCount(
            string? query,
            string? category,
            string? language,
            decimal? minPrice)
        {
            var list = SearchActiveOrders(cachedData.ActiveOrders, query, category, language, minPrice);

            return list.Count();
        }

        /// <summary>
        /// Find user by wallet address.
        /// </summary>
        /// <param name="address">Address of user's main wallet (in user-friendly form).</param>
        /// <param name="translateTo">Language (key or code/name) of language to translate to. Must match one of supported languages (from config).</param>
        [SwaggerResponse(400, "Address is empty or invalid.")]
        [HttpGet]
        public async Task<ActionResult<FindResult<User>>> FindUser(string address, string? translateTo = null)
        {
            if (string.IsNullOrEmpty(address))
            {
                ModelState.AddModelError(nameof(address), "Address is required.");
                return ValidationProblem();
            }
            else if (!TonUtils.Address.TrySetBounceable(address, false, out address))
            {
                ModelState.AddModelError(nameof(address), "Address not valid (wrong length, contains invalid characters, etc).");
            }

            Language? translateLanguage = default;
            if (!string.IsNullOrEmpty(translateTo))
            {
                translateLanguage = cachedData.AllLanguages.Find(x => x.Name == translateTo || x.Hash == translateTo);
                if (translateLanguage == null)
                {
                    ModelState.AddModelError(nameof(translateTo), "Unknown (unsupported) language value");
                }
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem();
            }

            var user = cachedData.AllUsers.Find(x => StringComparer.Ordinal.Equals(x.UserAddress, address));

            if (user != null && translateLanguage != null && user.AboutHash != null)
            {
                var translated = await dbProvider.MainDb.FindAsync<Translation>(x => x.Hash == user.AboutHash && x.Language == translateLanguage.Name);
                user.AboutTranslated = translated?.TranslatedText;
            }

            return new FindResult<User>(user);
        }

        /// <summary>
        /// Get user by index.
        /// </summary>
        /// <param name="index">ID of user ('index' field from user contract).</param>
        /// <param name="translateTo">Language (key or code/name) of language to translate to. Must match one of supported languages (from config).</param>
        [SwaggerResponse(400, "Index is invalid (or user does not exist).")]
        [HttpGet]
        public async Task<ActionResult<User>> GetUser(long index, string? translateTo = null)
        {
            var user = cachedData.AllUsers.Find(x => x.Index == index);

            if (user == null)
            {
                ModelState.AddModelError(nameof(index), "Invalid index (or user does not exist).");
                return ValidationProblem();
            }

            Language? translateLanguage = default;
            if (!string.IsNullOrEmpty(translateTo))
            {
                translateLanguage = cachedData.AllLanguages.Find(x => x.Name == translateTo || x.Hash == translateTo);
                if (translateLanguage == null)
                {
                    ModelState.AddModelError(nameof(translateTo), "Unknown (unsupported) language value");
                    return ValidationProblem();
                }
            }

            if (translateLanguage != null && user.AboutHash != null)
            {
                var translated = await dbProvider.MainDb.FindAsync<Translation>(x => x.Hash == user.AboutHash && x.Language == translateLanguage.Name);
                user.AboutTranslated = translated?.TranslatedText;
            }

            return user;
        }

        /// <summary>
        /// Get order by index.
        /// </summary>
        /// <param name="index">ID of order ('index' field from order contract).</param>
        /// <param name="translateTo">Language (key or code/name) of language to translate to. Must match one of supported languages (from config).</param>
        [SwaggerResponse(400, "Index is invalid (or order does not exist).")]
        [HttpGet]
        public async Task<ActionResult<Order>> GetOrder(long index, string? translateTo = null)
        {
            var order = cachedData.AllOrders.Find(x => x.Index == index);

            if (order == null)
            {
                ModelState.AddModelError(nameof(index), "Invalid index (or order does not exist).");
                return ValidationProblem();
            }

            Language? translateLanguage = default;
            if (!string.IsNullOrEmpty(translateTo))
            {
                translateLanguage = cachedData.AllLanguages.Find(x => x.Name == translateTo || x.Hash == translateTo);
                if (translateLanguage == null)
                {
                    ModelState.AddModelError(nameof(translateTo), "Unknown (unsupported) language value");
                    return ValidationProblem();
                }
            }

            if (translateLanguage != null)
            {
                if (order.NameHash != null)
                {
                    var translated = await dbProvider.MainDb.FindAsync<Translation>(x => x.Hash == order.NameHash && x.Language == translateLanguage.Name);
                    order.NameTranslated = translated?.TranslatedText;
                }
                if (order.DescriptionHash != null)
                {
                    var translated = await dbProvider.MainDb.FindAsync<Translation>(x => x.Hash == order.DescriptionHash && x.Language == translateLanguage.Name);
                    order.DescriptionTranslated = translated?.TranslatedText;
                }
                if (order.TechnicalTaskHash != null)
                {
                    var translated = await dbProvider.MainDb.FindAsync<Translation>(x => x.Hash == order.TechnicalTaskHash && x.Language == translateLanguage.Name);
                    order.TechnicalTaskTranslated = translated?.TranslatedText;
                }
            }

            return order;
        }

        /// <summary>
        /// Find order by contract address.
        /// </summary>
        /// <param name="address">Address of order contract (in user-friendly form).</param>
        /// <param name="translateTo">Language (key or code/name) of language to translate to. Must match one of supported languages (from config).</param>
        [SwaggerResponse(400, "Address is empty or invalid.")]
        [HttpGet]
        public async Task<ActionResult<FindResult<Order>>> FindOrder(string address, string? translateTo = null)
        {
            if (string.IsNullOrEmpty(address))
            {
                ModelState.AddModelError(nameof(address), "Address is required.");
                return ValidationProblem();
            }
            else if (!TonUtils.Address.TrySetBounceable(address, true, out address))
            {
                ModelState.AddModelError(nameof(address), "Address not valid (wrong length, contains invalid characters, etc).");
            }

            Language? translateLanguage = default;
            if (!string.IsNullOrEmpty(translateTo))
            {
                translateLanguage = cachedData.AllLanguages.Find(x => x.Name == translateTo || x.Hash == translateTo);
                if (translateLanguage == null)
                {
                    ModelState.AddModelError(nameof(translateTo), "Unknown (unsupported) language value");
                    return ValidationProblem();
                }
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem();
            }

            var order = cachedData.AllOrders.Find(x => StringComparer.Ordinal.Equals(x.Address, address));

            if (order != null && translateLanguage != null)
            {
                if (order.NameHash != null)
                {
                    var translated = await dbProvider.MainDb.FindAsync<Translation>(x => x.Hash == order.NameHash && x.Language == translateLanguage.Name);
                    order.NameTranslated = translated?.TranslatedText;
                }
                if (order.DescriptionHash != null)
                {
                    var translated = await dbProvider.MainDb.FindAsync<Translation>(x => x.Hash == order.DescriptionHash && x.Language == translateLanguage.Name);
                    order.DescriptionTranslated = translated?.TranslatedText;
                }
                if (order.TechnicalTaskHash != null)
                {
                    var translated = await dbProvider.MainDb.FindAsync<Translation>(x => x.Hash == order.TechnicalTaskHash && x.Language == translateLanguage.Name);
                    order.TechnicalTaskTranslated = translated?.TranslatedText;
                }
            }

            return new FindResult<Order>(order);
        }

        /// <summary>
        /// Get user statistics - number of orders, detailed by role (customer / freelancer) and by status.
        /// </summary>
        /// <param name="index">ID of user ('index' field from user contract).</param>
        /// <remarks>Only statuses with non-zero number of orders are returned.</remarks>
        [SwaggerResponse(400, "Index is invalid (or user does not exist).")]
        [HttpGet]
        public ActionResult<UserStat> GetUserStats(long index)
        {
            var user = cachedData.AllUsers.Find(x => x.Index == index);

            if (user == null)
            {
                ModelState.AddModelError(nameof(index), "Invalid index (or user does not exist).");
                return ValidationProblem();
            }

            var asCustomer = cachedData.AllOrders.Where(x => StringComparer.Ordinal.Equals(x.CustomerAddress, user.UserAddress));
            var asFreelancer = cachedData.AllOrders.Where(x => StringComparer.Ordinal.Equals(x.FreelancerAddress, user.UserAddress));

            var res = new UserStat()
            {
                AsCustomerByStatus = asCustomer.GroupBy(x => x.Status).ToDictionary(x => (int)x.Key, x => x.Count()),
                AsFreelancerByStatus = asFreelancer.GroupBy(x => x.Status).ToDictionary(x => (int)x.Key, x => x.Count()),
            };

            res.AsCustomerTotal = res.AsCustomerByStatus.Sum(x => x.Value);
            res.AsFreelancerTotal = res.AsFreelancerByStatus.Sum(x => x.Value);

            return res;
        }

        /// <summary>
        /// Get list of user orders by role and status.
        /// </summary>
        /// <param name="index">ID of user ('index' field from user contract).</param>
        /// <param name="role">Role of user: 'customer' or 'freelancer'.</param>
        /// <param name="status">Status of orders to return.</param>
        /// <param name="translateTo">Language (key or code/name) of language to translate to. Must match one of supported languages (from config).</param>
        [SwaggerResponse(400, "Invalid (nonexisting) 'index' or 'role' value.")]
        [HttpGet]
        public async Task<ActionResult<List<Order>>> GetUserOrders(long index, string role, int status, string? translateTo = null)
        {
            var mode = role.ToLowerInvariant() switch
            {
                "customer" => 1,
                "freelancer" => 2,
                _ => 0,
            };

            if (mode == 0)
            {
                ModelState.AddModelError(nameof(role), "Invalid 'role' value: use 'customer' or 'freelancer'.");
            }

            Language? translateLanguage = default;
            if (!string.IsNullOrEmpty(translateTo))
            {
                translateLanguage = cachedData.AllLanguages.Find(x => x.Name == translateTo || x.Hash == translateTo);
                if (translateLanguage == null)
                {
                    ModelState.AddModelError(nameof(translateTo), "Unknown (unsupported) language value");
                    return ValidationProblem();
                }
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem();
            }

            var user = cachedData.AllUsers.Find(x => x.Index == index);

            if (user == null)
            {
                ModelState.AddModelError(nameof(index), "Invalid index (or user does not exist).");
                return ValidationProblem();
            }

            var query = mode == 1
                ? cachedData.AllOrders.Where(x => StringComparer.Ordinal.Equals(x.CustomerAddress, user.UserAddress))
                : cachedData.AllOrders.Where(x => StringComparer.Ordinal.Equals(x.FreelancerAddress, user.UserAddress));

            var list = query.Where(x => x.Status == (OrderStatus)status).ToList();

            if (translateLanguage != null)
            {
                foreach (var order in list)
                {
                    if (order.NameHash != null)
                    {
                        var translated = await dbProvider.MainDb.FindAsync<Translation>(x => x.Hash == order.NameHash && x.Language == translateLanguage.Name);
                        order.NameTranslated = translated?.TranslatedText;
                    }
                    if (order.DescriptionHash != null)
                    {
                        var translated = await dbProvider.MainDb.FindAsync<Translation>(x => x.Hash == order.DescriptionHash && x.Language == translateLanguage.Name);
                        order.DescriptionTranslated = translated?.TranslatedText;
                    }
                    if (order.TechnicalTaskHash != null)
                    {
                        var translated = await dbProvider.MainDb.FindAsync<Translation>(x => x.Hash == order.TechnicalTaskHash && x.Language == translateLanguage.Name);
                        order.TechnicalTaskTranslated = translated?.TranslatedText;
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Get list of user activity in different orders.
        /// </summary>
        /// <param name="index">ID of user ('index' field from user contract).</param>
        /// <param name="page">Page number to return (default 0).</param>
        /// <param name="pageSize">Page size (default 10, max 100).</param>
        [SwaggerResponse(400, "Invalid (nonexisting) 'index' value.")]
        [HttpGet]
        public async Task<ActionResult<List<OrderActivity>>> GetUserActivity(
            [FromServices] IDbProvider dbProvider,
            long index,
            int page = 0,
            int pageSize = 10)
        {
            var user = cachedData.AllUsers.Find(x => x.Index == index);

            if (user == null)
            {
                ModelState.AddModelError(nameof(index), "Invalid index (or user does not exist).");
                return ValidationProblem();
            }

            var allOrders = cachedData.AllOrders;
            var list = await dbProvider.MainDb.Table<OrderActivity>()
                .Where(x => x.SenderAddress == user.UserAddress)
                .OrderByDescending(x => x.Timestamp)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var item in list)
            {
                item.Order = allOrders.Find(x => x.Id == item.OrderId);
            }

            return list;
        }

        /// <summary>
        /// Get list of order activity.
        /// </summary>
        /// <param name="index">ID of order ('index' field from user contract).</param>
        /// <param name="page">Page number to return (default 0).</param>
        /// <param name="pageSize">Page size (default 10, max 100).</param>
        [SwaggerResponse(400, "Invalid (nonexisting) 'index' value.")]
        [HttpGet]
        public async Task<ActionResult<List<OrderActivity>>> GetOrderActivity(
            [FromServices] IDbProvider dbProvider,
            long index,
            int page = 0,
            int pageSize = 10)
        {
            var order = cachedData.AllOrders.Find(x => x.Index == index);

            if (order == null)
            {
                ModelState.AddModelError(nameof(index), "Invalid index (or order does not exist).");
                return ValidationProblem();
            }

            var allUsers = cachedData.AllUsers;
            var list = await dbProvider.MainDb.Table<OrderActivity>()
                .Where(x => x.OrderId == order.Id)
                .OrderByDescending(x => x.Timestamp)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var item in list)
            {
                item.Sender = allUsers.Find(x => StringComparer.Ordinal.Equals(x.UserAddress, item.SenderAddress));
            }

            return list;
        }

        protected IEnumerable<Order> SearchActiveOrders(
            List<Order> source,
            string? query,
            string? category,
            string? language,
            decimal? minPrice)
        {
            var list = source.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                list = list.Where(x => string.Equals(x.Category, category, StringComparison.InvariantCultureIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(language))
            {
                list = list.Where(x => string.Equals(x.Language, language, StringComparison.InvariantCultureIgnoreCase));
            }

            if (minPrice != null)
            {
                list = list.Where(x => x.Price >= minPrice);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var words = query.ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                list = list.Where(x => Array.TrueForAll(words, z => x.TextToSearch.Contains(z, StringComparison.InvariantCulture)));
            }

            return list;
        }

        public class BackendConfig
        {
            public string MasterContractAddress { get; set; } = string.Empty;

            public bool Mainnet { get; set; }

            public List<Category> Categories { get; set; } = new();

            public List<Language> Languages { get; set; } = new();
        }

        public class UserStat
        {
            public int AsCustomerTotal { get; set; }

            public Dictionary<int, int> AsCustomerByStatus { get; set; } = new();

            public int AsFreelancerTotal { get; set; }

            public Dictionary<int, int> AsFreelancerByStatus { get; set; } = new();
        }

        public class FindResult<T>
            where T : class
        {
            public FindResult(T? data)
            {
                Found = data != null;
                Data = data;
            }

            public bool Found { get; set; }

            public T? Data { get; set; }
        }
    }
}
