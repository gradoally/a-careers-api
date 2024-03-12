using SQLite;

namespace SomeDAO.Backend.Data
{
    public class Settings
    {
        public const string KEY_DB_VERSION = "DB_VERSION";
        public const string MASTER_ADDRESS = "MASTER_ADDRESS";
        public const string LAST_MASTER_DATA_HASH = "LAST_MASTER_DATA_HASH";
        public const string LAST_MASTER_TX_LT = "LAST_MASTER_TX_LT";
        public const string NEXT_INDEX_ADMIN = "NEXT_INDEX_ADMIN";
        public const string NEXT_INDEX_USER = "NEXT_INDEX_USER";
        public const string NEXT_INDEX_ORDER = "NEXT_INDEX_ORDER";
        public const string IN_MAINNET = "IN_MAINNET";
        public const string LAST_SEQNO = "LAST_SEQNO";
        public const string IGNORE_NOTIFICATIONS_BEFORE = "IGNORE_NOTIFICATIONS_BEFORE";

        [Obsolete("For data layer only")]
        public Settings()
        {
            Id = string.Empty;
        }

        public Settings(string id, int value)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            Id = id;
            IntValue = value;
        }

        public Settings(string id, long value)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            Id = id;
            LongValue = value;
        }

        public Settings(string id, string value)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            Id = id;
            StringValue = value;
        }

        public Settings(string id, DateTimeOffset value)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            Id = id;
            DateTimeOffsetValue = value;
        }

        public Settings(string id, bool value)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            Id = id;
            BoolValue = value;
        }

        [PrimaryKey]
        public string Id { get; set; }

        public string? StringValue { get; set; }

        public int? IntValue { get; set; }

        public long? LongValue { get; set; }

        public bool? BoolValue { get; set; }

        public DateTimeOffset? DateTimeOffsetValue { get; set; }
    }
}
