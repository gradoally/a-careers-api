using System.Globalization;
using Microsoft.Extensions.Options;
using RecurrentTasks;

namespace SomeDAO.Backend.Services
{
    public class HealthReportTask : IRunnable
    {
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(9);

        private readonly ILogger logger;
        private readonly IConfiguration configuration;
        private readonly string path;
        private readonly IHttpClientFactory httpClientFactory;

        public HealthReportTask(ILogger<HealthReportTask> logger, IConfiguration configuration, IOptions<BackendOptions> backendOptions, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.path = backendOptions.Value.HealthReportPath;
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var baseUrl = configuration["Kestrel:Endpoints:Http:Url"];
            var uri = baseUrl + path;

            logger.LogTrace("Sending POST to {Uri}", uri);

            var list = GetData(scopeServiceProvider);

            using var httpClient = httpClientFactory.CreateClient();
            using var resp = await httpClient.PostAsJsonAsync(uri, list);
            resp.EnsureSuccessStatusCode();
        }

        private static List<TaskInfo> GetData(IServiceProvider scopeServiceProvider)
        {
            var list = new List<TaskInfo>();

            foreach (var taskType in StartupIndexer.RegisteredTasks)
            {
                var name = taskType.GenericTypeArguments[0].Name;
                var task = (ITask)scopeServiceProvider.GetRequiredService(taskType);
                if (task.RunStatus.LastSuccessTime.Add(task.Options.Interval).Add(task.Options.Interval) < DateTimeOffset.Now)
                {
                    list.Add(new TaskInfo()
                    {
                        Name = name,
                        Ok = false,
                        Comment = $"failed with {task.RunStatus.LastException?.GetType().Name}, last success {task.RunStatus.LastSuccessTime.UtcDateTime.ToString("u", CultureInfo.InvariantCulture)}",
                    });
                }
                else
                {
                    list.Add(new TaskInfo()
                    {
                        Name = name,
                        Ok = true,
                        Comment = task.RunStatus.LastSuccessTime.UtcDateTime.ToString("u", CultureInfo.InvariantCulture),
                    });
                }
            }

            return list;
        }

        public class TaskInfo
        {
            public string Name { get; set; } = string.Empty;

            public bool Ok { get; set; }

            public string Comment { get; set; } = string.Empty;
        }
    }
}
