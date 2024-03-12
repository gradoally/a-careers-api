using SQLite;

namespace SomeDAO.Backend.Data
{
    public class Translation
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        [NotNull, Indexed(Name = "Hash_Lang", Unique = true, Order = 1)]
        public byte[] Hash { get; set; } = Array.Empty<byte>();

        [NotNull, Indexed(Name = "Hash_Lang", Unique = true, Order = 2)]
        public string Language { get; set; } = string.Empty;

        public string TranslatedText { get; set; } = string.Empty;

        public DateTimeOffset Timestamp { get; set; }
    }
}
