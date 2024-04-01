using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
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
        private const int MinPageSize = 10;
        private const int MaxPageSize = 100;

        private readonly CachedData cachedData;
        private readonly Lazy<IDbProvider> lazyDbProvider;

        public SearchController(CachedData searchService, Lazy<IDbProvider> lazyDbProvider)
        {
            this.cachedData = searchService ?? throw new ArgumentNullException(nameof(searchService));
            this.lazyDbProvider = lazyDbProvider ?? throw new ArgumentNullException(nameof(lazyDbProvider));
        }

        /// <summary>
        /// Returns general configuration data.
        /// </summary>
        [HttpGet]
        public ActionResult<BackendConfig> Config()
        {
            return new BackendConfig
            {
                MasterContractAddress = cachedData.MasterAddress,
                Mainnet = cachedData.InMainnet,
                Categories = cachedData.AllCategories,
                Languages = cachedData.AllLanguages,
            };
        }

        /// <summary>
        /// Returns some statistics.
        /// </summary>
        /// <remarks>
        /// Drill-down lists only display items with a non-zero value.
        /// </remarks>
        [HttpGet]
        public ActionResult<BackendStatistics> Stat()
        {
            return new BackendStatistics
            {
                OrderCount = cachedData.AllOrders.Count,
                OrderCountByStatus = cachedData.OrderCountByStatus,
                OrderCountByCategory = cachedData.OrderCountByCategory,
                OrderCountByLanguage = cachedData.OrderCountByLanguage,
                UserCount = cachedData.AllUsers.Count,
                UserCountByStatus = cachedData.UserCountByStatus,
                UserCountByLanguage = cachedData.UserCountByLanguage,
            };
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
            [Range(0, int.MaxValue)] int page = 0,
            [Range(MinPageSize, MaxPageSize)] int pageSize = MinPageSize)
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
        public ActionResult<FindResult<User>> FindUser([Required(AllowEmptyStrings = false)] string address, string? translateTo = null)
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
                user = user.ShallowCopy();
                var db = lazyDbProvider.Value.MainDb;
                var translated = db.Find<Translation>(x => x.Hash == user.AboutHash && x.Language == translateLanguage.Name);
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
        public ActionResult<User> GetUser([Required] long index, string? translateTo = null)
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
                user = user.ShallowCopy();
                var db = lazyDbProvider.Value.MainDb;
                var translated = db.Find<Translation>(x => x.Hash == user.AboutHash && x.Language == translateLanguage.Name);
                user.AboutTranslated = translated?.TranslatedText;
            }

            return user;
        }

        /// <summary>
        /// Get order by index.
        /// </summary>
        /// <param name="index">ID of order ('index' field from order contract).</param>
        /// <param name="translateTo">Language (key or code/name) of language to translate to. Must match one of supported languages (from config).</param>
        /// <param name="currentUserIndex">Index of current/active user (to have non-null <see cref="Order.CurrentUserResponse"/> in response).</param>
        [SwaggerResponse(400, "Index is invalid (or order/user does not exist).")]
        [HttpGet]
        public ActionResult<Order> GetOrder([Required] long index, string? translateTo = null, long? currentUserIndex = null)
        {
            var order = cachedData.AllOrders.Find(x => x.Index == index);

            if (order == null)
            {
                ModelState.AddModelError(nameof(index), "Invalid index (or order does not exist).");
                return ValidationProblem();
            }

            var user = currentUserIndex == null ? default : cachedData.AllUsers.Find(x => x.Index == currentUserIndex);
            if (user == null && currentUserIndex != null)
            {
                ModelState.AddModelError(nameof(currentUserIndex), "Invalid index (or user does not exist).");
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

            order = order.ShallowCopy();

            if (translateLanguage != null)
            {
                var db = lazyDbProvider.Value.MainDb;

                if (order.NameHash != null)
                {
                    var translated = db.Find<Translation>(x => x.Hash == order.NameHash && x.Language == translateLanguage.Name);
                    order.NameTranslated = translated?.TranslatedText;
                }
                if (order.DescriptionHash != null)
                {
                    var translated = db.Find<Translation>(x => x.Hash == order.DescriptionHash && x.Language == translateLanguage.Name);
                    order.DescriptionTranslated = translated?.TranslatedText;
                }
                if (order.TechnicalTaskHash != null)
                {
                    var translated = db.Find<Translation>(x => x.Hash == order.TechnicalTaskHash && x.Language == translateLanguage.Name);
                    order.TechnicalTaskTranslated = translated?.TranslatedText;
                }
            }

            if (user != null)
            {
                var db = lazyDbProvider.Value.MainDb;
                order.CurrentUserResponse = db.Table<OrderResponse>().FirstOrDefault(x => x.OrderId == order.Id && x.FreelancerAddress == user.UserAddress);
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
        public ActionResult<FindResult<Order>> FindOrder([Required(AllowEmptyStrings = false)] string address, string? translateTo = null)
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
                order = order.ShallowCopy();

                var db = lazyDbProvider.Value.MainDb;

                if (order.NameHash != null)
                {
                    var translated = db.Find<Translation>(x => x.Hash == order.NameHash && x.Language == translateLanguage.Name);
                    order.NameTranslated = translated?.TranslatedText;
                }
                if (order.DescriptionHash != null)
                {
                    var translated = db.Find<Translation>(x => x.Hash == order.DescriptionHash && x.Language == translateLanguage.Name);
                    order.DescriptionTranslated = translated?.TranslatedText;
                }
                if (order.TechnicalTaskHash != null)
                {
                    var translated = db.Find<Translation>(x => x.Hash == order.TechnicalTaskHash && x.Language == translateLanguage.Name);
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
        public ActionResult<UserStat> GetUserStats([Required] long index)
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
        public ActionResult<List<Order>> GetUserOrders(
            [Required] long index,
            [Required(AllowEmptyStrings = false)] string role,
            [Required] int status,
            string? translateTo = null)
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

            var list = query.Where(x => x.Status == status).ToList();

            if (translateLanguage != null)
            {
                list = list.Select(x => x.ShallowCopy()).ToList();

                var db = lazyDbProvider.Value.MainDb;

                foreach (var order in list)
                {
                    if (order.NameHash != null)
                    {
                        var translated = db.Find<Translation>(x => x.Hash == order.NameHash && x.Language == translateLanguage.Name);
                        order.NameTranslated = translated?.TranslatedText;
                    }
                    if (order.DescriptionHash != null)
                    {
                        var translated = db.Find<Translation>(x => x.Hash == order.DescriptionHash && x.Language == translateLanguage.Name);
                        order.DescriptionTranslated = translated?.TranslatedText;
                    }
                    if (order.TechnicalTaskHash != null)
                    {
                        var translated = db.Find<Translation>(x => x.Hash == order.TechnicalTaskHash && x.Language == translateLanguage.Name);
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
        public ActionResult<List<OrderActivity>> GetUserActivity(
            [Required] long index,
            [Range(0, int.MaxValue)] int page = 0,
            [Range(MinPageSize, MaxPageSize)] int pageSize = MinPageSize)
        {
            var user = cachedData.AllUsers.Find(x => x.Index == index);

            if (user == null)
            {
                ModelState.AddModelError(nameof(index), "Invalid index (or user does not exist).");
                return ValidationProblem();
            }

            var allOrders = cachedData.AllOrders;
            var db = lazyDbProvider.Value.MainDb;
            var list = db.Table<OrderActivity>()
                .Where(x => x.SenderAddress == user.UserAddress)
                .OrderByDescending(x => x.Timestamp)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            foreach (var item in list)
            {
                item.Order = allOrders.Find(x => x.Id == item.OrderId);
            }

            return list;
        }

        /// <summary>
        /// Get list of order activity.
        /// </summary>
        /// <param name="index">ID of order ('index' field from order contract).</param>
        /// <param name="page">Page number to return (default 0).</param>
        /// <param name="pageSize">Page size (default 10, max 100).</param>
        [SwaggerResponse(400, "Invalid (nonexisting) 'index' value.")]
        [HttpGet]
        public ActionResult<List<OrderActivity>> GetOrderActivity(
            [Required] long index,
            [Range(0, int.MaxValue)] int page = 0,
            [Range(MinPageSize, MaxPageSize)] int pageSize = MinPageSize)
        {
            var order = cachedData.AllOrders.Find(x => x.Index == index);

            if (order == null)
            {
                ModelState.AddModelError(nameof(index), "Invalid index (or order does not exist).");
                return ValidationProblem();
            }

            var allUsers = cachedData.AllUsers;
            var db = lazyDbProvider.Value.MainDb;
            var list = db.Table<OrderActivity>()
                .Where(x => x.OrderId == order.Id)
                .OrderByDescending(x => x.Timestamp)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            foreach (var item in list)
            {
                item.Sender = allUsers.Find(x => StringComparer.Ordinal.Equals(x.UserAddress, item.SenderAddress));
            }

            return list;
        }

        /// <summary>
        /// Get list of order responses.
        /// </summary>
        /// <param name="index">ID of order ('index' field from order contract).</param>
        /// <remarks>
        /// Responses are sorted by price (high prices first). There can be no more than 255 responses,
        ///   so no paging is used (always all responses are returned), and they may be sorted client-side when needed.
        /// </remarks>
        [SwaggerResponse(400, "Invalid (nonexisting) 'index' value.")]
        [HttpGet]
        public ActionResult<List<OrderResponse>> GetOrderResponses([Required] long index)
        {
            var order = cachedData.AllOrders.Find(x => x.Index == index);

            if (order == null)
            {
                ModelState.AddModelError(nameof(index), "Invalid index (or order does not exist).");
                return ValidationProblem();
            }

            var db = lazyDbProvider.Value.MainDb;
            var list = db.Table<OrderResponse>()
                .Where(x => x.OrderId == order.Id)
                .OrderByDescending(x => x.Price)
                .ToList();

            foreach (var item in list)
            {
                item.Freelancer = cachedData.AllUsers.Find(x => StringComparer.Ordinal.Equals(x.UserAddress, item.FreelancerAddress));
            }

            return list;
        }

        /// <summary>
        /// Get list of orders.
        /// </summary>
        /// <remarks>Translations and inner object are not provided!</remarks>
        /// <param name="status">Status of returned orders.</param>
        /// <param name="category">Category of returned orders.</param>
        /// <param name="language">Language of returned orders.</param>
        /// <param name="sort">Sort order: 'asc' or 'desc'.</param>
        /// <param name="page">Page number to return (default 0).</param>
        /// <param name="pageSize">Page size (default 10, max 100).</param>
        [SwaggerResponse(400, "Invalid request.")]
        [HttpGet]
        public ActionResult<List<Order>> ListOrders(
            int? status,
            string? category,
            string? language,
            string sort = "asc",
            [Range(0, int.MaxValue)] int page = 0,
            [Range(MinPageSize, MaxPageSize)] int pageSize = MinPageSize)
        {
            var sortMode = sort.ToLowerInvariant() switch
            {
                "asc" => 1,
                "desc" => 2,
                _ => 0,
            };

            if (sortMode == 0)
            {
                ModelState.AddModelError(nameof(sort), "Invalid value. Use 'asc' or 'desc'.");
                return ValidationProblem();
            }

            var list = cachedData.AllOrders
                .Where(x => status == null || x.Status == status)
                .Where(x => string.IsNullOrEmpty(category) || x.Category == category)
                .Where(x => string.IsNullOrEmpty(language) || x.Language == language);

            var sorted = sortMode == 1 ? list.OrderBy(x => x.Index) : list.OrderByDescending(x => x.Index);

            return sorted.Skip(page * pageSize).Take(pageSize)
                .Select(x => x.ShallowCopy())
                .Select(x => { x.Customer = null; x.Freelancer = null; return x; })
                .ToList();
        }

        /// <summary>
        /// Get list of users.
        /// </summary>
        /// <remarks>Translations and inner object are not provided!</remarks>
        /// <param name="status">Status of returned users ('active' or 'moderation' or 'banned').</param>
        /// <param name="language">Language of returned users.</param>
        /// <param name="sort">Sort order: 'asc' or 'desc'.</param>
        /// <param name="page">Page number to return (default 0).</param>
        /// <param name="pageSize">Page size (default 10, max 100).</param>
        [SwaggerResponse(400, "Invalid request.")]
        [HttpGet]
        public ActionResult<List<User>> ListUsers(
            string? status,
            string? language,
            string sort = "asc",
            [Range(0, int.MaxValue)] int page = 0,
            [Range(MinPageSize, MaxPageSize)] int pageSize = MinPageSize)
        {
            if (status != null)
            {
                var statusValue = status.ToLowerInvariant() switch
                {
                    Data.User.StatusActive => Data.User.StatusActive,
                    Data.User.StatusModeration => Data.User.StatusModeration,
                    Data.User.StatusBanned => Data.User.StatusBanned,
                    _ => null,
                };

                if (statusValue == null)
                {
                    ModelState.AddModelError(nameof(status), "Invalid value. Use 'active' or 'moderation' or 'banned'.");
                    return ValidationProblem();
                }
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
                return ValidationProblem();
            }

            var list = cachedData.AllUsers
                .Where(x => status == null || x.UserStatus == status)
                .Where(x => string.IsNullOrEmpty(language) || x.Language == language);

            var sorted = sortMode == 1 ? list.OrderBy(x => x.Index) : list.OrderByDescending(x => x.Index);

            return sorted.Skip(page * pageSize).Take(pageSize).ToList();
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

        public class BackendStatistics
        {
            public int OrderCount { get; set; }

            public Dictionary<int, int> OrderCountByStatus { get; set; } = new();

            public Dictionary<string, int> OrderCountByCategory { get; set; } = new();

            public Dictionary<string, int> OrderCountByLanguage { get; set; } = new();

            public int UserCount { get; set; }

            public Dictionary<string, int> UserCountByStatus { get; set; } = new();

            public Dictionary<string, int> UserCountByLanguage { get; set; } = new();
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
