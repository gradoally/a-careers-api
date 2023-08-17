using Microsoft.Extensions.Options;
using RecurrentTasks;
using SomeDAO.Backend.Data;
using TonLibDotNet;

namespace SomeDAO.Backend.Services
{
    public class ItemUpdateChecker : IRunnable
    {
        private static readonly TimeSpan HaveMoreDataInterval = TimeSpan.FromSeconds(5);

        private static readonly int MaxBatch = 19;

        private readonly ILogger logger;
        private readonly IDbProvider dbProvider;
        private readonly BackendOptions options;
        private readonly IDataParser dataParser;

        public ItemUpdateChecker(ILogger<ItemUpdateChecker> logger, IDbProvider dbProvider, IOptions<BackendOptions> options, IDataParser dataParser)
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
                currentTask.Options.Interval = haveMore ? HaveMoreDataInterval : options.ItemUpdateCheckerInterval;
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
        }

        protected async Task<bool> RunImplAsync()
        {
            var counter = 0;
            while (counter < MaxBatch)
            {
                counter++;

                var needUpdate = await dbProvider.MainDb.Table<NftItem>().OrderBy(x => x.LastUpdate).FirstOrDefaultAsync(x => x.UpdateNeeded);
                if (needUpdate == null)
                {
                    logger.LogDebug("Update queue is empty");
                    return false;
                }

                counter++;

                var item = await dataParser.GetNftItem(needUpdate.Address).ConfigureAwait(false);

                if (item == null)
                {
                    logger.LogWarning("NFT {Address} failed to update (null returned)", needUpdate.Address);
                }
                else
                {
                    await dbProvider.MainDb.UpdateAsync(item).ConfigureAwait(false);
                    logger.LogInformation("NFT {Address} updated", item.Address);
                }
            }

            return true;
        }
    }
}
