using SQLite;

namespace SomeDAO.Backend.Data
{
    public class SyncQueueItem
    {
        public SyncQueueItem()
        {
            // Nothing
        }

        public SyncQueueItem(IBlockchainEntity entity, DateTimeOffset? minLastSync = null)
        {
            Index = entity.Index;
            EntityType = entity.EntityType;
            SyncAt = DateTimeOffset.UtcNow;
            RetryCount = 0;
            MinLastSync = minLastSync ?? DateTimeOffset.MinValue;
        }

        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        [NotNull]
        public long Index { get; set; }

        [NotNull]
        public EntityType EntityType { get; set; }

        [Indexed]
        public DateTimeOffset SyncAt { get; set; }

        [NotNull]
        public int RetryCount { get; set; }

        [NotNull]
        public DateTimeOffset MinLastSync { get; set; }
    }
}
