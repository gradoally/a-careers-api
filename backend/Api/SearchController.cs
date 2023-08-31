using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SomeDAO.Backend.Data;
using SomeDAO.Backend.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace SomeDAO.Backend.Api
{
    [ApiController]
    [Consumes(System.Net.Mime.MediaTypeNames.Application.Json)]
    [Produces(System.Net.Mime.MediaTypeNames.Application.Json)]
    [Route("/api/[action]")]
    [SwaggerResponse(200, "Request is accepted, processed and response contains requested data.")]
    [SwaggerResponse(400, "Request is invalid (wrong structure, unauthorized etc).")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService searchService;
        private readonly BackendOptions options;

        public SearchController(ISearchService searchService, IOptions<BackendOptions> options)
        {
            this.searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Returns some statistics.
        /// </summary>
        [HttpGet]
        public ActionResult<Statistics> Stat()
        {
            var res = new Statistics()
            {
                Collection = options.CollectionAddress,
                OrdersTotal = searchService.Count,
            };

            return res;
        }

        /// <summary>
        /// Returns top
        /// </summary>
        /// <param name="query">Free query</param>
        /// <param name="status">Show only specified status.</param>
        /// <param name="category">Show only specified category.</param>
        /// <param name="minAmount"></param>
        /// <param name="maxAmount"></param>
        /// <param name="orderBy">Sort field: 'creation_unix_time' or 'starting_unix_time' or 'ending_unix_time'.</param>
        /// <param name="sort">Sort order: 'asc' or 'desc'.</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<List<Order>> Search(
            string? query,
            string? status,
            string? category,
            decimal? minAmount,
            decimal? maxAmount,
            string? orderBy = DataParser.PropNameCreateUnixTime,
            string? sort = ISearchService.OrderAsc)
        {
            if (!DataParser.PropNameCreateUnixTime.Equals(orderBy, StringComparison.InvariantCultureIgnoreCase) &&
                !DataParser.PropNameStartUnixTime.Equals(orderBy, StringComparison.InvariantCultureIgnoreCase) &&
                !DataParser.PropNameEndUnixTime.Equals(orderBy, StringComparison.InvariantCultureIgnoreCase))
            {
                return BadRequest($"Invalid 'orderBy' value (expected '{DataParser.PropNameCreateUnixTime}' or '{DataParser.PropNameStartUnixTime}' or '{DataParser.PropNameEndUnixTime}')");
            }

            if (!ISearchService.OrderAsc.Equals(sort, StringComparison.InvariantCultureIgnoreCase) &&
                !ISearchService.OrderDesc.Equals(sort, StringComparison.InvariantCultureIgnoreCase))
            {
                return BadRequest($"Invalid 'sort' value (expected '{ISearchService.OrderAsc}' or '{ISearchService.OrderDesc}')");
            }

            return searchService.Find(query?.Trim(), status?.Trim(), category?.Trim(), minAmount, maxAmount, orderBy, orderBy).ToList();
        }

        public class Statistics
        {
            public string Collection { get; set; } = string.Empty;

            public int OrdersTotal { get; set; }
        }
    }
}
