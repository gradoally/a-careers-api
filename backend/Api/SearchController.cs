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
        /// Statistics.
        /// </summary>
        [HttpGet]
        public Statistics Stat()
        {
            var res = new Statistics()
            {
                Collection = options.CollectionAddress,
                OrdersTotal = searchService.Count,
            };

            return res;
        }

        /// <summary>
        /// Search.
        /// </summary>
        [HttpGet]
        public List<Order> Search(string text)
        {
            return searchService.Find(text).ToList();
        }

        public class Statistics
        {
            public string Collection { get; set; } = string.Empty;

            public int OrdersTotal { get; set; }
        }
    }
}
