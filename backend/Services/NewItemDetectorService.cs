using System.Numerics;
using Microsoft.Extensions.Options;
using RecurrentTasks;
using SomeDAO.Backend.Data;
using TonLibDotNet;

namespace SomeDAO.Backend.Services
{
    public class NewItemDetectorService : IRunnable
    {
        private readonly ILogger logger;
        private readonly IDbProvider dbProvider;
        private readonly ITonClient tonClient;
        private readonly BackendOptions options;

        public NewItemDetectorService(ILogger<NewItemDetectorService> logger, IDbProvider dbProvider, ITonClient tonClient, IOptions<BackendOptions> options)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.tonClient = tonClient ?? throw new ArgumentNullException(nameof(tonClient));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            try
            {
                await RunImplAsync(cancellationToken);
                currentTask.Options.Interval = options.NewItemDetectorInterval;
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

        protected async Task RunImplAsync(CancellationToken cancellationToken)
        {
            await tonClient.InitIfNeeded().ConfigureAwait(false);

            logger.LogDebug("Reading collection data from {Address}", options.CollectionAddress);

            var (nextItemIndex, _, _) = await TonRecipes.NFTs.GetCollectionData(tonClient, options.CollectionAddress).ConfigureAwait(false);

            var nextIndex = (int)nextItemIndex;
            var lastIndexInNetwork = nextIndex - 1;

            logger.LogDebug("Next item index in smc is: {Value}", nextIndex);

            var last = await dbProvider.MainDb.Table<NftItem>().OrderByDescending(x => x.Index).FirstOrDefaultAsync().ConfigureAwait(false);
            var lastIndexInDb = last?.Index ?? -1;

            if (lastIndexInDb == lastIndexInNetwork)
            {
                logger.LogInformation("Last known NFT index is already {Value}, there is no new NFTs, nothing to load.", lastIndexInDb);
                return;
            }

            logger.LogDebug("Need to download {Count} new NFTs", lastIndexInNetwork - lastIndexInDb);

            while (lastIndexInDb < lastIndexInNetwork)
            {
                lastIndexInDb++;

                var adr = await TonRecipes.NFTs.GetNftAddressByIndex(tonClient, options.CollectionAddress, new BigInteger(lastIndexInDb)).ConfigureAwait(false);

                logger.LogDebug("NFT #{Index} address is {Address}", lastIndexInDb, adr);

                var item = new NftItem
                {
                    Index = lastIndexInDb,
                    Address = adr
                };

                var info = await TonRecipes.NFTs.GetNftData(tonClient, adr).ConfigureAwait(false);
                item.Init = info.init;
                item.OwnerAddress = info.ownerAddress;
                item.LastUpdate = DateTimeOffset.UtcNow;

                await dbProvider.MainDb.InsertAsync(item).ConfigureAwait(false);

                logger.LogInformation("NFT #{Index} (address {Address}) added, owner is {Owner}", item.Index, item.Address, item.OwnerAddress);
            }
        }
    }
}
