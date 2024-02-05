using SomeDAO.Backend.Data;
using TonLibDotNet.Requests;

namespace SomeDAO.Backend.Services
{
    public class SyncSchedulerService
    {
        private readonly ILogger logger;
        private readonly IDbProvider dbProvider;

        public SyncSchedulerService(ILogger<SyncSchedulerService> logger, IDbProvider dbProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
        }

        public Task Schedule(IBlockchainEntity entity)
        {
            return Schedule(entity, DateTimeOffset.MinValue);
        }

        public async Task Schedule(IBlockchainEntity entity, DateTimeOffset minTimestamp)
        {
            var db = dbProvider.MainDb;

            var item = await db.Table<SyncQueueItem>().FirstOrDefaultAsync(x => x.EntityType == entity.EntityType && x.Index == entity.Index);
            if (item == null)
            {
                item = new SyncQueueItem(entity, minTimestamp);
                await db.InsertAsync(item).ConfigureAwait(false);
                logger.LogTrace("Sync for {EntityType} #{Index} scheduled (min = {Min}).", item.EntityType, item.Index, item.MinLastSync);
            }
            else
            {
                // Set MinLastSync to max(existing, requested)
                if (minTimestamp > item.MinLastSync)
                {
                    item.MinLastSync = minTimestamp;
                }

                // Set to sync NOW, but do not reset RetryCount,
                //   so will return to "expected" delay in case of failure.
                item.SyncAt = DateTimeOffset.UtcNow;

                await db.InsertOrReplaceAsync(item).ConfigureAwait(false);
                logger.LogTrace("Sync for {EntityType} #{Index} rescheduled (min = {Min}, retries = {Count}).", item.EntityType, item.Index, item.MinLastSync, item.RetryCount);
            }
        }

        public async Task ScheduleMaster()
        {
            var db = dbProvider.MainDb;
            var item = await db.Table<SyncQueueItem>().FirstOrDefaultAsync(x => x.EntityType == EntityType.Master && x.Index == 0);
            item ??= new SyncQueueItem()
            {
                EntityType = EntityType.Master,
                Index = 0,
            };

            item.SyncAt = DateTimeOffset.UtcNow;

            await db.InsertOrReplaceAsync(item).ConfigureAwait(false);
            logger.LogTrace("Sync for {EntityType} #{Index} rescheduled (min = {Min}, retries = {Count}).", item.EntityType, item.Index, item.MinLastSync, item.RetryCount);
        }
    }
}
