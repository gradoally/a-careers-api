using System.Text.Json.Serialization;
using SomeDAO.Backend.Services;
using SQLite;

namespace SomeDAO.Backend.Data
{
    public class Order
    {
        [PrimaryKey]
        public long Index { get; set; }

        [NotNull, Indexed(Unique = true)]
        public string Address { get; set; } = string.Empty;

        [Indexed]
        public string? OwnerAddress { get; set; }

        [JsonPropertyName(DataParser.PropNameImage)]
        public string? Image { get; set; }

        [JsonPropertyName(DataParser.PropNameStatus)]
        public string? Status { get; set; }

        [JsonPropertyName(DataParser.PropNameName)]
        public string? Name { get; set; }

        [JsonPropertyName(DataParser.PropNameAmount)]
        public decimal Amount { get; set; }

        [JsonPropertyName(DataParser.PropNameDescription)]
        public string? Description { get; set; }

        [JsonPropertyName(DataParser.PropNameTechAssignment)]
        public string? Assignment { get; set; }

        [JsonPropertyName(DataParser.PropNameCategory)]
        public string? Category { get; set; }

        [JsonPropertyName(DataParser.PropNameCustomer)]
        public string? Customer { get; set; }

        [JsonPropertyName(DataParser.PropNameCreateUnixTime)]
        public DateTimeOffset Created { get; set; }

        [JsonPropertyName(DataParser.PropNameStartUnixTime)]
        public DateTimeOffset Starting { get; set; }

        [JsonPropertyName(DataParser.PropNameEndUnixTime)]
        public DateTimeOffset Ending { get; set; }

        [JsonIgnore]
        public string? LastTxHash { get; set; }

        [JsonIgnore]
        public long LastTxLt { get; set; }

        [JsonIgnore]
        [NotNull, Indexed]
        public DateTimeOffset LastUpdate { get; set; }

        [JsonIgnore]
        [NotNull, Indexed]
        public DateTimeOffset UpdateAfter { get; set; }

        [JsonIgnore]
        private string? textToSearch = null;

        [JsonIgnore]
        [Ignore]
        public string TextToSearch
        {
            get
            {
                textToSearch ??= Name?.ToUpperInvariant() + " " + Description?.ToUpperInvariant();
                return textToSearch;
            }
        }
    }
}
