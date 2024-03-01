using Microsoft.Extensions.Options;
using RecurrentTasks;
using SomeDAO.Backend.Data;

namespace SomeDAO.Backend.Services
{
    public class NotificationTask : IRunnable
    {
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan HaveMoreDataInterval = TimeSpan.FromSeconds(2);

        private const int MaxBatch = 50;

        private readonly ILogger logger;
        private readonly IDbProvider dbProvider;
        private readonly CachedData cachedData;
        private readonly BackendOptions options;
        private readonly IHttpClientFactory httpClientFactory;

        public NotificationTask(ILogger<NotificationTask> logger, IDbProvider dbProvider, CachedData cachedData, IOptions<BackendOptions> options, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.cachedData = cachedData ?? throw new ArgumentNullException(nameof(cachedData));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(options.NotificationsEndpoint))
            {
                logger.LogWarning("Endpoint not set, notifications disabled");
                return;
            }

            currentTask.Options.Interval = RetryInterval;

            using var httpClient = httpClientFactory.CreateClient();

            var db = dbProvider.MainDb;

            var ignoreBefore = db.Find<Settings>(Settings.IGNORE_NOTIFICATIONS_BEFORE)?.DateTimeOffsetValue;

            var counter = 0;
            while (counter < MaxBatch && !cancellationToken.IsCancellationRequested)
            {
                var next = db.Table<NotificationQueueItem>().OrderBy(x => x.TxTime).FirstOrDefault();

                if (next == null)
                {
                    logger.LogDebug("No [more] notifications.");
                    currentTask.Options.Interval = DefaultInterval;
                    break;
                }

                counter++;

                if (ignoreBefore == null || next.TxTime < ignoreBefore)
                {
                    logger.LogInformation("Notification about activity #{Id} ignored (too old)", next.OrderActivityId);
                }
                else
                {
                    var activity = db.Find<OrderActivity>(next.OrderActivityId);
                    if (activity != null)
                    {
                        activity.Order = cachedData.AllOrders.Find(x => x.Id == activity.OrderId);

                        using var content = JsonContent.Create(activity);
                        using var req = new HttpRequestMessage(HttpMethod.Post, options.NotificationsEndpoint) { Content = content };
                        using var resp = await httpClient.SendAsync(req, cancellationToken);
                        resp.EnsureSuccessStatusCode();

                        logger.LogDebug("Notification about activity #{Id} sent Ok", activity.Id);
                    }
                }

                db.Delete(next);
            }

            if (counter >= MaxBatch)
            {
                currentTask.Options.Interval = HaveMoreDataInterval;
            }
        }
    }
}
