using System.Text.Json.Serialization;
using SQLite;

namespace SomeDAO.Backend.Data
{
    public class User : IUserContent, IBlockchainEntity
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
        /// User wallet address - in non-bounceable form.
        /// </summary>
        [NotNull, Indexed]
        public string UserAddress { get; set; } = string.Empty;

        public DateTimeOffset? RevokedAt { get; set; }

        #region IUserContent

        public bool IsUser { get; set; }

        public bool IsFreelancer { get; set; }

        public string? Nickname { get; set; }

        public string? Telegram { get; set; }

        public string? About { get; set; }

        public string? Website { get; set; }

        public string? Portfolio { get; set; }

        public string? Resume { get; set; }

        public string? Specialization { get; set; }

        public string? Language { get; set; }

        #endregion

        [JsonIgnore]
        public byte[]? AboutHash { get; set; }

        [Ignore]
        public string? AboutTranslated { get; set; }

        [JsonIgnore]
        public bool NeedTranslation { get; set; }

        #region IBlockchainEntity

        [JsonIgnore]
        public EntityType EntityType { get; } = EntityType.User;

        [JsonIgnore]
        public long LastTxLt { get; set; }

        [JsonIgnore]
        public string? LastTxHash { get; set; }

        [JsonIgnore]
        public DateTimeOffset LastSync { get; set; }

        #endregion
    }
}
