using SomeDAO.Backend.Data;

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

        public void Schedule(IBlockchainEntity entity)
        {
            Schedule(entity, DateTimeOffset.MinValue);
        }

        public void Schedule(IBlockchainEntity entity, DateTimeOffset minTimestamp)
        {
            var db = dbProvider.MainDb;

            var item = db.Table<SyncQueueItem>().FirstOrDefault(x => x.EntityType == entity.EntityType && x.Index == entity.Index);
            if (item == null)
            {
                item = new SyncQueueItem(entity, minTimestamp);
                db.Insert(item);
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

                db.InsertOrReplace(item);
                logger.LogTrace("Sync for {EntityType} #{Index} rescheduled (min = {Min}, retries = {Count}).", item.EntityType, item.Index, item.MinLastSync, item.RetryCount);
            }
        }

        public void ScheduleMaster()
        {
            var db = dbProvider.MainDb;
            var item = db.Table<SyncQueueItem>().FirstOrDefault(x => x.EntityType == EntityType.Master && x.Index == 0);
            item ??= new SyncQueueItem()
            {
                EntityType = EntityType.Master,
                Index = 0,
            };

            item.SyncAt = DateTimeOffset.UtcNow;

            db.InsertOrReplace(item);
            logger.LogTrace("Sync for {EntityType} #{Index} [re]scheduled (min = {Min}, retries = {Count}).", item.EntityType, item.Index, item.MinLastSync, item.RetryCount);
        }
    }
}
