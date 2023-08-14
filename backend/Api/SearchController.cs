using Microsoft.AspNetCore.Mvc;
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
    }
}
