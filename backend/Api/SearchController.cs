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
		private readonly SearchService searchService;
		private readonly BackendConfig backendConfig;

        public SearchController(ILogger<SearchController> logger, SearchService searchService, IOptions<BackendOptions> backendOptions, IOptions<TonOptions> tonOptions)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            this.backendConfig = new BackendConfig
            {
				MasterContractAddress = backendOptions.Value.MasterAddress,
                Mainnet = tonOptions.Value.UseMainnet,
            };
        }

        /// <summary>
        /// Returns general configuration data.
        /// </summary>
        [HttpGet]
        public ActionResult<BackendConfig> Config()
        {
            return backendConfig;
        }

		/// <summary>
		/// Returns Orders that meet filter.
		/// </summary>
		/// <param name="query">Free query</param>
		/// <param name="category">Show only specified category.</param>
		/// <param name="language">Show only specified language.</param>
		/// <param name="minPrice">Minimum price to include</param>
		/// <param name="orderBy">Sort field: 'createdAt' or 'deadline'.</param>
		/// <param name="sort">Sort order: 'asc' or 'desc'.</param>
		/// <param name="page">Page number to return (default 0).</param>
		/// <param name="pageSize">Page size (default 10, max 100).</param>
		[SwaggerResponse(400, "Invalid request.")]
		[HttpGet]
		public ActionResult<List<Order>> Search(
			string? query,
			string? category,
			string? language,
			decimal? minPrice,
			string orderBy = "createdAt",
			string sort = "asc",
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

			var list = searchService.FindOrders(query, category, language, minPrice);

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
		/// Returns number of Orders that meet filter.
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
			var list = searchService.FindOrders(query, category, language, minPrice);

			return list.Count();
		}

		/// <summary>
		/// Find user by wallet address.
		/// </summary>
		/// <param name="address">Address of user's main wallet (in user-friendly form).</param>
		[SwaggerResponse(400, "Address is empty or invalid.")]
		[SwaggerResponse(404, "User not found.")]
		[HttpGet]
        public ActionResult<User> FindUser(string address)
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

			if (!ModelState.IsValid)
			{
				return ValidationProblem();
			}

			var user = searchService.AllUsers.Find(x => StringComparer.Ordinal.Equals(x.UserAddress, address));

			if (user == null)
            {
				return NotFound("User not found");
			}

			return user;
        }

		/// <summary>
		/// Get user by index.
		/// </summary>
		/// <param name="index">ID of user ('index' field from user contract).</param>
		[SwaggerResponse(404, "User not found.")]
		[HttpGet]
		public ActionResult<User> GetUser(long index)
		{
			var user = searchService.AllUsers.Find(x => x.Index == index);

			if (user == null)
			{
				return NotFound("User not found");
			}

			return user;
		}

		/// <summary>
		/// Get order by index.
		/// </summary>
		/// <param name="index">ID of order ('index' field from order contract).</param>
		[SwaggerResponse(404, "Order not found.")]
		[HttpGet]
		public ActionResult<Order> GetOrder(long index)
		{
			var order = searchService.AllOrders.Find(x => x.Index == index);

			if (order == null)
			{
				return NotFound("Order not found");
			}

			return order;
		}

		/// <summary>
		/// Get user statistics - number of orders, detailed by role (customer / freelancer) and by status.
		/// </summary>
		/// <param name="index">ID of user ('index' field from user contract).</param>
		/// <remarks>Only statuses with non-zero number of orders are returned.</remarks>
		[SwaggerResponse(404, "User not found.")]
		[HttpGet]
		public ActionResult<UserStat> GetUserStats(long index)
		{
			var user = searchService.AllUsers.Find(x => x.Index == index);

			if (user == null)
			{
				return NotFound("User not found");
			}

			var asCustomer = searchService.AllOrders.Where(x => StringComparer.Ordinal.Equals(x.CustomerAddress, user.UserAddress));
			var asFreelancer = searchService.AllOrders.Where(x => StringComparer.Ordinal.Equals(x.FreelancerAddress, user.UserAddress));

			var res = new UserStat()
			{
				AsCustomerByStatus = asCustomer.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count()),
				AsFreelancerByStatus = asFreelancer.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count()),
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
		/// <remarks>Only statuses with non-zero number of orders are returned.</remarks>
		[SwaggerResponse(400, "Invalid 'role' value.")]
		[SwaggerResponse(404, "User not found.")]
		[HttpGet]
		public ActionResult<List<Order>> GetUserOrders(long index, string role, int status)
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

			if (!ModelState.IsValid)
			{
				return ValidationProblem();
			}

			var user = searchService.AllUsers.Find(x => x.Index == index);

			if (user == null)
			{
				return NotFound("User not found");
			}

			var query = mode == 1
				? searchService.AllOrders.Where(x => StringComparer.Ordinal.Equals(x.CustomerAddress, user.UserAddress))
				: searchService.AllOrders.Where(x => StringComparer.Ordinal.Equals(x.FreelancerAddress, user.UserAddress));

			var list = query.Where(x => x.Status == status).ToList();

			return list;
		}

		public class BackendConfig
        {
            public string MasterContractAddress { get; set; } = string.Empty;

            public bool Mainnet { get; set; }
        }

		public class UserStat
		{
			public int AsCustomerTotal { get; set; }

			public Dictionary<int, int> AsCustomerByStatus { get; set; } = new();

			public int AsFreelancerTotal { get; set; }

			public Dictionary<int, int> AsFreelancerByStatus { get; set; } = new();
		}
	}
}
