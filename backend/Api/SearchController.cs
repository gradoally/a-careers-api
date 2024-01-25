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
    [SwaggerResponse(400, "Request is invalid (wrong structure, unauthorized etc).")]
    [SwaggerResponse(404, "Requested item was not found.")]
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
        /// Returns some statistics.
        /// </summary>
        [HttpGet]
        public ActionResult<BackendConfig> Config()
        {
            return backendConfig;
        }

        /// <summary>
        /// Find user by his wallet address.
        /// </summary>
        [HttpGet]
        public ActionResult<User> FindUser(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                ModelState.AddModelError(nameof(address), "Value is required.");
                return ValidationProblem();
            }

            if (!TonUtils.Address.TrySetBounceable(address, true, out address))
            {
				ModelState.AddModelError(nameof(address), "Not invalid (wrong length, contains invalid characters, etc).");
				return ValidationProblem();
			}

			var user = searchService.FindUser(address);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        ///// <summary>
        ///// Returns top
        ///// </summary>
        ///// <param name="query">Free query</param>
        ///// <param name="status">Show only specified status.</param>
        ///// <param name="category">Show only specified category.</param>
        ///// <param name="minAmount"></param>
        ///// <param name="maxAmount"></param>
        ///// <param name="orderBy">Sort field: 'creation_unix_time' or 'starting_unix_time' or 'ending_unix_time'.</param>
        ///// <param name="sort">Sort order: 'asc' or 'desc'.</param>
        ///// <returns></returns>
        //[HttpGet]
        //public ActionResult<List<Order>> Search(
        //    string? query,
        //    string? status,
        //    string? category,
        //    decimal? minAmount,
        //    decimal? maxAmount,
        //    string? orderBy = DataParser.PropNameCreateUnixTime,
        //    string? sort = SearchService.OrderAsc)
        //{
        //    if (!DataParser.PropNameCreateUnixTime.Equals(orderBy, StringComparison.InvariantCultureIgnoreCase) &&
        //        !DataParser.PropNameStartUnixTime.Equals(orderBy, StringComparison.InvariantCultureIgnoreCase) &&
        //        !DataParser.PropNameEndUnixTime.Equals(orderBy, StringComparison.InvariantCultureIgnoreCase))
        //    {
        //        return BadRequest($"Invalid 'orderBy' value (expected '{DataParser.PropNameCreateUnixTime}' or '{DataParser.PropNameStartUnixTime}' or '{DataParser.PropNameEndUnixTime}')");
        //    }

        //    if (!SearchService.OrderAsc.Equals(sort, StringComparison.InvariantCultureIgnoreCase) &&
        //        !SearchService.OrderDesc.Equals(sort, StringComparison.InvariantCultureIgnoreCase))
        //    {
        //        return BadRequest($"Invalid 'sort' value (expected '{SearchService.OrderAsc}' or '{SearchService.OrderDesc}')");
        //    }

        //    return searchService.Find(query?.Trim(), status?.Trim(), category?.Trim(), minAmount, maxAmount, orderBy, orderBy).ToList();
        //}

        public class BackendConfig
        {
            public string MasterContractAddress { get; set; } = string.Empty;

            public bool Mainnet { get; set; }
        }
    }
}
