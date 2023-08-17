using Microsoft.AspNetCore.Mvc;
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

        public SearchController(ISearchService searchService)
        {
            this.searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        }

        /// <summary>
        /// API access check.
        /// </summary>
        /// <remarks>Returns "Pong" text. Maybe used for health-check.</remarks>
        [HttpGet]
        [Produces(System.Net.Mime.MediaTypeNames.Text.Plain)]
        [SwaggerResponse(200, "Successful execution. Response text equals to \"Pong\".")]
        public ActionResult Ping()
        {
            return Ok("Pong");
        }

        /// <summary>
        /// Search.
        /// </summary>
        [HttpGet]
        public ActionResult Search(string text)
        {
            return Ok(searchService.Find(text).Select(x => new SearchResultItem(x)));
        }

        public class SearchResultItem
        {
            public SearchResultItem(NftItem nft)
            {
                Id = nft.Index;
                Address = nft.Address;
                OwnerAddress = nft.OwnerAddress;
            }

            public int Id { get; set; }

            public string Address { get; set; }

            public string? OwnerAddress { get; set; }
        }
    }
}
