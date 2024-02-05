using SQLite;

namespace SomeDAO.Backend.Data
{
    public class Settings
    {
        public const string KEY_DB_VERSION = "DB_VERSION";
        public const string LAST_MASTER_DATA_HASH = "LAST_MASTER_DATA_HASH";
        public const string LAST_MASTER_TX_LT = "LAST_MASTER_TX_LT";
        public const string NEXT_INDEX_ADMIN = "NEXT_INDEX_ADMIN";
        public const string NEXT_INDEX_USER = "NEXT_INDEX_USER";
        public const string NEXT_INDEX_ORDER = "NEXT_INDEX_ORDER";

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

        [PrimaryKey]
        public string Id { get; set; }

        public string? StringValue { get; set; }

        public int? IntValue { get; set; }

        public long? LongValue { get; set; }

        public DateTimeOffset? DateTimeOffsetValue { get; set; }
    }
}
