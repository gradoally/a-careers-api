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

        [NotNull]
        public bool Init { get; set; }

        public string? OwnerAddress { get; set; }
    }
}
