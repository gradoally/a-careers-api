using System.Numerics;
using Microsoft.Extensions.Options;
using RecurrentTasks;
using SomeDAO.Backend.Data;
using TonLibDotNet;

namespace SomeDAO.Backend.Services
{
    public class NewOrdersDetector : IRunnable
    {
        private readonly ILogger logger;
        private readonly IDbProvider dbProvider;
        private readonly ITonClient tonClient;
        private readonly IDataParser dataParser;
        private readonly BackendOptions options;

        public NewOrdersDetector(ILogger<NewOrdersDetector> logger, IDbProvider dbProvider, ITonClient tonClient, IOptions<BackendOptions> options, IDataParser dataParser)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.tonClient = tonClient ?? throw new ArgumentNullException(nameof(tonClient));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.dataParser = dataParser ?? throw new ArgumentNullException(nameof(dataParser));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            try
            {
                await RunImplAsync();
                currentTask.Options.Interval = options.NewOrdersDetectorInterval;
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

        protected async Task RunImplAsync()
        {
            await tonClient.InitIfNeeded().ConfigureAwait(false);

            var (nextItemIndex, _, _) = await TonRecipes.NFTs.GetCollectionData(tonClient, options.CollectionAddress).ConfigureAwait(false);

            var lastIndexInNetwork = (int)nextItemIndex - 1;

            logger.LogDebug("Result of get_collection_data() on {Address}: nextItemIndex={Value}, so I should have Order with index={Value} ", options.CollectionAddress, nextItemIndex, lastIndexInNetwork);

            var last = await dbProvider.MainDb.Table<Order>().OrderByDescending(x => x.Index).FirstOrDefaultAsync().ConfigureAwait(false);
            var lastIndexInDb = last?.Index ?? -1;

            if (lastIndexInDb == lastIndexInNetwork)
            {
                logger.LogInformation("Last known Order index is {Value}, so there are no new Orders, nothing to load.", lastIndexInDb);
                return;
            }

            logger.LogDebug("Need to download {Count} new Orders", lastIndexInNetwork - lastIndexInDb);

            while (lastIndexInDb < lastIndexInNetwork)
            {
                lastIndexInDb++;

                var adr = await TonRecipes.NFTs.GetNftAddressByIndex(tonClient, options.CollectionAddress, new BigInteger(lastIndexInDb)).ConfigureAwait(false);

                logger.LogDebug("Order #{Index} address is {Address}", lastIndexInDb, adr);

                var item = await dataParser.GetNftItem(adr).ConfigureAwait(false);

                await dbProvider.MainDb.InsertAsync(item).ConfigureAwait(false);

                logger.LogInformation("Order #{Index} (address {Address}) added, owner is {Owner}", item.Index, item.Address, item.OwnerAddress);
            }
        }
    }
}
