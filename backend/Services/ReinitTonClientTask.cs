using RecurrentTasks;
using TonLibDotNet;

namespace SomeDAO.Backend.Services
{
    public class ReinitTonClientTask : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        private readonly ILogger logger;
        private readonly ITonClient tonClient;

        public ReinitTonClientTask(ILogger<ReinitTonClientTask> logger, ITonClient tonClient)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.tonClient = tonClient ?? throw new ArgumentNullException(nameof(tonClient));
        }

        public Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            tonClient.Deinit();
            logger.LogDebug("TonClient de-inited");
            return Task.CompletedTask;
        }
    }
}
