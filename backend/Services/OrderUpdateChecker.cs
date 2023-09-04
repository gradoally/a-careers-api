using Microsoft.Extensions.Options;
using RecurrentTasks;
using SomeDAO.Backend.Data;
using TonLibDotNet;

namespace SomeDAO.Backend.Services
{
    public class OrderUpdateChecker : IRunnable
    {
        private static readonly TimeSpan HaveMoreDataInterval = TimeSpan.FromSeconds(5);

        private static readonly int MaxBatch = 19;

        private readonly ILogger logger;
        private readonly IDbProvider dbProvider;
        private readonly BackendOptions options;
        private readonly DataParser dataParser;

        public OrderUpdateChecker(ILogger<OrderUpdateChecker> logger, IDbProvider dbProvider, IOptions<BackendOptions> options, DataParser dataParser)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.dataParser = dataParser ?? throw new ArgumentNullException(nameof(dataParser));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            try
            {
                var haveMore = await RunImplAsync();
                currentTask.Options.Interval = haveMore ? HaveMoreDataInterval : options.OrderUpdateCheckerInterval;
            }
            catch (TonClientException ex)
            {
                if (currentTask.RunStatus.FailsCount < 5)
                {
                    logger.LogError(ex, "TonClient exception catched. Will retry in a moment.");
                    currentTask.Options.Interval = TimeSpan.FromSeconds(3);
                }
                else
                {
                    logger.LogError(
                        ex,
                        "TonClient exception catched, again. Too many fails ({Count}), will retry later (with usual interval).",
                        currentTask.RunStatus.FailsCount);
                }
            }
            finally
            {
                scopeServiceProvider.GetRequiredService<ITask<SearchService>>().TryRunImmediately();
            }
        }

        protected async Task<bool> RunImplAsync()
        {
            var counter = 0;
            while (counter < MaxBatch)
            {
                counter++;

                var needUpdate = await dbProvider.MainDb.Table<Order>()
                    .OrderBy(x => x.UpdateAfter)
                    .FirstOrDefaultAsync(x => x.UpdateAfter > DateTimeOffset.MinValue);
                if (needUpdate == null)
                {
                    logger.LogDebug("Update queue is empty");
                    return false;
                }

                // move to end of queue (in case something is wrong with it)
                needUpdate.UpdateAfter = DateTimeOffset.UtcNow;
                await dbProvider.MainDb.UpdateAsync(needUpdate).ConfigureAwait(false);

                var item = await dataParser.GetNftItem(needUpdate.Address).ConfigureAwait(false);
                if (item == null)
                {
                    logger.LogWarning("Order {Address} failed to update (null returned)", needUpdate.Address);
                }
                else
                {
                    await dbProvider.MainDb.UpdateAsync(item).ConfigureAwait(false);
                    logger.LogInformation("Order {Address} updated", item.Address);
                }
            }

            return true;
        }
    }
}
