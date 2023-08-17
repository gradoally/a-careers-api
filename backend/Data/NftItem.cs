using SQLite;

namespace SomeDAO.Backend.Data
{
    public class NftItem
    {
        [PrimaryKey]
        public int Index { get; set; }

        [NotNull, Indexed(Unique = true)]
        public string Address { get; set; } = string.Empty;

        [NotNull, Indexed]
        public DateTimeOffset LastUpdate { get; set; }

        public string? OwnerAddress { get; set; }

        public string? LastTxHash { get; set; }

        public long LastTxLt { get; set; }

        [NotNull, Indexed]
        public bool UpdateNeeded { get; set; }
    }
}
