using Microsoft.Extensions.Options;
using RecurrentTasks;
using SomeDAO.Backend.Data;
using TonLibDotNet;

namespace SomeDAO.Backend.Services
{
    public class DevInitService : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

        private readonly ILogger logger;
        private readonly IDbProvider dbProvider;
        private readonly ITask task;
        private readonly BackendOptions options;
        private readonly SyncSchedulerService syncScheduler;

        public DevInitService(ILogger<DevInitService> logger, IDbProvider dbProvider, ITask<SyncTask> task, IOptions<BackendOptions> options, SyncSchedulerService syncScheduler)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.task = task ?? throw new ArgumentNullException(nameof(task));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.syncScheduler = syncScheduler ?? throw new ArgumentNullException(nameof(syncScheduler));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var db = dbProvider.MainDb;

            var admin = await db.FindAsync<Admin>(x => x.Index == 0);
            if (admin == null)
            {
                admin = new Admin()
                {
                    Index = 0,
                    Address = TonUtils.Address.SetBounceable("EQDMMZ8op6c6G9tMNVgLs0-DkijEztCmTkPGd18RhYnjt76F", true),
                    AdminAddress = options.MasterAddress,
                };
                await db.InsertAsync(admin).ConfigureAwait(false);
            }

            var user = await db.FindAsync<User>(x => x.Index == 0);
            if (user == null)
            {
                user = new User()
                {
                    Index = 0,
                    Address = TonUtils.Address.SetBounceable("EQCk_PlzthTcQWbOXe_bhbWs6nsgwAWOdwF-mwHTF5vBYO0s", true),
                    UserAddress = options.MasterAddress,
                };
                await db.InsertAsync(user).ConfigureAwait(false);
            }

            var order = await db.FindAsync<Order>(x => x.Index == 0);
            if (order == null)
            {
                order = new Order()
                {
                    Index = 0,
                    Address = TonUtils.Address.SetBounceable("EQAup_18ePpROCkOKYj6o3IBrElsq5osmgAj-_276gPWHNQi", true),
                    CustomerAddress = options.MasterAddress,
                };
                await db.InsertAsync(order).ConfigureAwait(false);
            }

            await syncScheduler.Schedule(admin);
            await syncScheduler.Schedule(user);
            await syncScheduler.Schedule(order);

            logger.LogInformation("Initial data saved into DB");

            currentTask.Options.Interval = TimeSpan.Zero;

            task.TryRunImmediately();
        }
    }
}
