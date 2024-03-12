using SQLite;

namespace SomeDAO.Backend.Data
{
    public class NotificationQueueItem
    {
        public NotificationQueueItem()
        {
            // Nothing
        }

        public NotificationQueueItem(long orderActivityId, DateTimeOffset transactionTime)
        {
            OrderActivityId = orderActivityId;
            TxTime = transactionTime;
        }

        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        [NotNull]
        public long OrderActivityId { get; set; }

        [NotNull, Indexed]
        public DateTimeOffset TxTime { get; set; }
    }
}
