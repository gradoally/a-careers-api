using RecurrentTasks;
using SomeDAO.Backend.Data;
using TonLibDotNet;
using TonLibDotNet.Types;
using TonLibDotNet.Types.Internal;

namespace SomeDAO.Backend.Services
{
    public class MasterTrackerTask : IRunnable
    {
        private readonly ILogger logger;
        private readonly ITonClient tonClient;
        private readonly IDbProvider dbProvider;
        private readonly SyncSchedulerService syncScheduler;
        private readonly CachedData cachedData;
        private readonly DataParser dataParser;
        private readonly ITask syncTask;

        public MasterTrackerTask(ILogger<MasterTrackerTask> logger, ITonClient tonClient, IDbProvider dbProvider, SyncSchedulerService syncScheduler, CachedData cachedData, DataParser dataParser, ITask<SyncTask> syncTask)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.tonClient = tonClient ?? throw new ArgumentNullException(nameof(tonClient));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.syncScheduler = syncScheduler ?? throw new ArgumentNullException(nameof(syncScheduler));
            this.cachedData = cachedData ?? throw new ArgumentNullException(nameof(cachedData));
            this.dataParser = dataParser ?? throw new ArgumentNullException(nameof(dataParser));
            this.syncTask = syncTask ?? throw new ArgumentNullException(nameof(syncTask));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var lastSeqnoSetting = await dbProvider.MainDb.FindAsync<Settings>(Settings.LAST_SEQNO).ConfigureAwait(false);
            var newSeqno = await dataParser.EnsureSynced(lastSeqnoSetting?.LongValue ?? 0).ConfigureAwait(false);
            await dbProvider.MainDb.InsertOrReplaceAsync(new Settings(Settings.LAST_SEQNO, newSeqno)).ConfigureAwait(false);

            var masterAddress = await dbProvider.MainDb.FindAsync<Settings>(Settings.MASTER_ADDRESS).ConfigureAwait(false);

            var state = await tonClient.RawGetAccountState(masterAddress.StringValue!).ConfigureAwait(false);

            var dataHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Convert.FromBase64String(state.Data)));
            var storedHash = await dbProvider.MainDb.FindAsync<Settings>(Settings.LAST_MASTER_DATA_HASH).ConfigureAwait(false);

            var needSync = false;

            if (dataHash != storedHash?.StringValue)
            {
                await syncScheduler.ScheduleMaster().ConfigureAwait(false);
                logger.LogDebug("Master data hash mismatch, sync queued (stored {Value}, actual {Value}).", storedHash?.StringValue, dataHash);
                needSync = true;
            }

            var endLt = await dbProvider.MainDb.FindAsync<Settings>(Settings.LAST_MASTER_TX_LT).ConfigureAwait(false);
            if (endLt == null || state.LastTransactionId.Lt != endLt.LongValue)
            {
                var found = 0;
                var start = new TransactionId() { Lt = state.LastTransactionId.Lt, Hash = state.LastTransactionId.Hash };
                await foreach (var tx in dataParser.EnumerateTransactions(masterAddress.StringValue!, start, endLt?.LongValue ?? 0))
                {
                    IEnumerable<AccountAddress> arr = new[] { tx.InMsg!.Source };
                    if (tx.OutMsgs != null)
                    {
                        arr = arr.Concat(tx.OutMsgs.Select(x => x.Destination));
                    }

                    foreach (var item in arr)
                    {
                        var adr = TonUtils.Address.SetBounceable(item.Value, true);

                        var a = cachedData.AllAdmins.Find(x => StringComparer.Ordinal.Equals(x.Address, adr));
                        if (a != null)
                        {
                            await syncScheduler.Schedule(a).ConfigureAwait(false);
                            found++;
                        }

                        var u = cachedData.AllUsers.Find(x => StringComparer.Ordinal.Equals(x.Address, adr));
                        if (u != null)
                        {
                            await syncScheduler.Schedule(u).ConfigureAwait(false);
                            found++;
                        }

                        var o = cachedData.AllOrders.Find(x => StringComparer.Ordinal.Equals(x.Address, adr));
                        if (o != null)
                        {
                            await syncScheduler.Schedule(o).ConfigureAwait(false);
                            found++;
                        }
                    }
                }

                await dbProvider.MainDb.InsertOrReplaceAsync(new Settings(Settings.LAST_MASTER_TX_LT, state.LastTransactionId.Lt)).ConfigureAwait(false);
                logger.LogDebug("Checked transactions to Lt={Lt}, found {Count} known addresses (non unique!) for sync.", state.LastTransactionId.Lt, found);
                needSync |= found > 0;
            }

            if (needSync)
            {
                syncTask.TryRunImmediately();
            }
        }
    }
}
