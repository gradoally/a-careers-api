using Microsoft.Extensions.Options;
using RecurrentTasks;
using SomeDAO.Backend.Data;

namespace SomeDAO.Backend.Services
{
    public class ForceResyncTask : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

        private readonly ILogger logger;
        private readonly BackendOptions options;
        private readonly IDbProvider dbProvider;
        private readonly SyncSchedulerService syncScheduler;
        private readonly ITask syncTask;

        public ForceResyncTask(ILogger<ForceResyncTask> logger, IOptions<BackendOptions> options, IDbProvider dbProvider, SyncSchedulerService syncScheduler, ITask<SyncTask> syncTask)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.syncScheduler = syncScheduler ?? throw new ArgumentNullException(nameof(syncScheduler));
            this.syncTask = syncTask ?? throw new ArgumentNullException(nameof(syncTask));
        }

        public Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var c1 = ForceResyncAdmins();
            var c2 = ForceResyncUsers();
            var c3 = ForceResyncOrders();

            logger.LogDebug("Scheduled {Count} Admins, {Count} Users, {Count} Orders for resync", c1, c2, c3);

            if (c1 != 0 || c2 != 0 || c3 != 0)
            {
                syncTask.TryRunImmediately();
            }
            else
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
            }

            return Task.CompletedTask;
        }

        protected int ForceResyncAdmins()
        {
            var boundary = DateTimeOffset.UtcNow.Subtract(options.AdminForceResyncInterval);
            var list = dbProvider.MainDb.Table<Admin>().Where(x => x.LastSync < boundary).ToList();

            foreach (var item in list)
            {
                syncScheduler.Schedule(item);
            }

            return list.Count;
        }

        protected int ForceResyncUsers()
        {
            var boundary = DateTimeOffset.UtcNow.Subtract(options.UserForceResyncInterval);
            var list = dbProvider.MainDb.Table<User>().Where(x => x.LastSync < boundary).ToList();

            foreach (var item in list)
            {
                syncScheduler.Schedule(item);
            }

            return list.Count;
        }

        protected int ForceResyncOrders()
        {
            var boundary = DateTimeOffset.UtcNow.Subtract(options.OrderForceResyncInterval);
            var list = dbProvider.MainDb.Table<Order>().Where(x => x.LastSync < boundary).ToList();

            foreach (var item in list)
            {
                syncScheduler.Schedule(item);
            }

            return list.Count;
        }
    }
}
