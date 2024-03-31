using System.Text.Json;
using Microsoft.Extensions.Options;
using SomeDAO.Backend.Services;

namespace SomeDAO.Backend
{
    public class IndexerHealthUpdateMiddleware : IMiddleware
    {
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        private readonly PathString path;

        public IndexerHealthUpdateMiddleware(IOptions<BackendOptions> options)
        {
            this.path = options?.Value.HealthReportPath ?? default;
        }

        public static List<HealthReportTask.TaskInfo>? Data { get; private set; }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var req = context.Request;

            if (req.Method != "POST" || !path.HasValue || !path.Equals(context.Request.Path))
            {
                await next(context);
            }
            else
            {
                Data = await JsonSerializer.DeserializeAsync<List<HealthReportTask.TaskInfo>>(req.Body, jsonOptions);
            }
        }
    }
}
