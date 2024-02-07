using System.Text.Json.Serialization;
using SQLite;

namespace SomeDAO.Backend.Data
{
    public class Language
    {
        [JsonPropertyName("key")]
        [PrimaryKey]
        public string Hash { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        [NotNull]
        public string Name { get; set; } = string.Empty;
    }
}
