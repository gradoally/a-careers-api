using System.Text.Json;
using SQLite;

namespace SomeDAO.Backend.Data
{
    public class NotificationQueueItem
    {
        private static readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web);

        public NotificationQueueItem()
        {
            // Nothing
        }

        public NotificationQueueItem(OrderActivity orderActivity, Order order)
        {
            ArgumentNullException.ThrowIfNull(orderActivity);
            ArgumentNullException.ThrowIfNull(order);

            OrderActivityId = orderActivity.Id;
            TxTime = orderActivity.Timestamp;

            orderActivity.Order = order;
            Body = JsonSerializer.Serialize(orderActivity, options);
        }

        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        [NotNull]
        public long OrderActivityId { get; set; }

        [NotNull, Indexed]
        public DateTimeOffset TxTime { get; set; }

        [NotNull]
        public string Body { get; set; } = string.Empty;
    }
}
