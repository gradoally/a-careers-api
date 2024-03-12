using System.Text.Json.Serialization;
using SQLite;

namespace SomeDAO.Backend.Data
{
    public class Admin : IAdminContent, IBlockchainEntity
    {
        [JsonIgnore]
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        [NotNull, Indexed(Unique = true)]
        public long Index { get; set; }

        /// <summary>
        /// Smartcontract address - in bounceable form.
        /// </summary>
        [NotNull, Indexed(Unique = true)]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Admin wallet address - in non-bounceable form.
        /// </summary>
        [NotNull, Indexed]
        public string AdminAddress { get; set; } = string.Empty;

        public DateTimeOffset? RevokedAt { get; set; }

        #region IAdminContent

        public string? Category { get; set; }

        public bool CanApproveUser { get; set; }

        public bool CanRevokeUser { get; set; }

        public string? Nickname { get; set; }

        public string? About { get; set; }

        public string? Website { get; set; }

        public string? Portfolio { get; set; }

        public string? Resume { get; set; }

        public string? Specialization { get; set; }

        #endregion

        #region IBlockchainEntity

        [JsonIgnore]
        public EntityType EntityType { get; } = EntityType.Admin;

        [JsonIgnore]
        public long LastTxLt { get; set; }

        [JsonIgnore]
        public string? LastTxHash { get; set; }

        [JsonIgnore]
        public DateTimeOffset LastSync { get; set; }

        #endregion
    }
}
