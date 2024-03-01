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

        public static long LastKnownSeqno { get; private set; }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var lastSeqno = dbProvider.MainDb.Find<Settings>(Settings.LAST_SEQNO);
            var lastSeqnoValue = lastSeqno?.LongValue ?? 0;

            var seqno = await dataParser.EnsureSynced(lastSeqnoValue);

            // Write occasionally to not spam our DB
            if (seqno - lastSeqnoValue > 100)
            {
                lastSeqno ??= new Settings(Settings.LAST_SEQNO, 0L);
                lastSeqno.LongValue = seqno;
                dbProvider.MainDb.InsertOrReplace(lastSeqno);
            }

            LastKnownSeqno = seqno;

            var masterAddress = dbProvider.MainDb.Find<Settings>(Settings.MASTER_ADDRESS);
            var state = await tonClient.RawGetAccountState(masterAddress!.StringValue!);

            var lastTxLt = state.LastTransactionId.Lt;
            var dataHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Convert.FromBase64String(state.Data)));

            var storedHash = dbProvider.MainDb.Find<Settings>(Settings.LAST_MASTER_DATA_HASH);
            var storedLt = dbProvider.MainDb.Find<Settings>(Settings.LAST_MASTER_TX_LT);

            var needSync = false;

            if (dataHash != storedHash?.StringValue)
            {
                logger.LogDebug("Master data hash mismatch, sync queued (stored {Value}, actual {Value}).", storedHash?.StringValue, dataHash);
                needSync = true;
            }

            if (lastTxLt != storedLt?.LongValue)
            {
                logger.LogDebug("Master last TX lt mismatch, sync queued (stored {Value}, actual {Value}).", storedLt?.LongValue, lastTxLt);
                needSync = true;
            }

            if (needSync)
            {
                syncScheduler.ScheduleMaster();
                syncTask.TryRunImmediately();
            }
        }
    }
}
