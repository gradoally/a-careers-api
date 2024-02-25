using RecurrentTasks;
using SomeDAO.Backend.Data;
using TonLibDotNet;

namespace SomeDAO.Backend.Services
{
    public class MasterTrackerTask : IRunnable
    {
        private readonly ILogger logger;
        private readonly ITonClient tonClient;
        private readonly IDbProvider dbProvider;
        private readonly SyncSchedulerService syncScheduler;
        private readonly DataParser dataParser;
        private readonly ITask syncTask;

        public MasterTrackerTask(ILogger<MasterTrackerTask> logger, ITonClient tonClient, IDbProvider dbProvider, SyncSchedulerService syncScheduler, DataParser dataParser, ITask<SyncTask> syncTask)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.tonClient = tonClient ?? throw new ArgumentNullException(nameof(tonClient));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.syncScheduler = syncScheduler ?? throw new ArgumentNullException(nameof(syncScheduler));
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
            var storedLt = await dbProvider.MainDb.FindAsync<Settings>(Settings.LAST_MASTER_TX_LT).ConfigureAwait(false);

            var needSync = false;

            if (dataHash != storedHash?.StringValue)
            {
                logger.LogDebug("Master data hash mismatch, sync queued (stored {Value}, actual {Value}).", storedHash?.StringValue, dataHash);
                needSync = true;
            }

            if (state.LastTransactionId.Lt != storedLt?.LongValue)
            {
                logger.LogDebug("Master last TX lt mismatch, sync queued (stored {Value}, actual {Value}).", storedLt?.LongValue, state.LastTransactionId.Lt);
                needSync = true;
            }

            if (needSync)
            {
                await syncScheduler.ScheduleMaster().ConfigureAwait(false);
                syncTask.TryRunImmediately();
            }
        }
    }
}
