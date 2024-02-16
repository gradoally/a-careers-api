using RecurrentTasks;
using SomeDAO.Backend.Data;
using TonLibDotNet;

namespace SomeDAO.Backend.Services
{
    public class ReinitTask : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        private readonly ILogger logger;
        private readonly ITonClient tonClient;
        private readonly IDbProvider dbProvider;

        public ReinitTask(ILogger<ReinitTask> logger, ITonClient tonClient, IDbProvider dbProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.tonClient = tonClient ?? throw new ArgumentNullException(nameof(tonClient));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            tonClient.Deinit();
            logger.LogDebug("TonClient de-inited");

            await dbProvider.Reconnect();
            logger.LogDebug("DbProvider reconnected");
        }
    }
}
