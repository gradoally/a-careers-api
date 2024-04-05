namespace SomeDAO.Backend.Data
{
    using System.Text.Json.Serialization;
    using SQLite;

    public class OrderResponse
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        [JsonIgnore]
        [NotNull, Indexed]
        public long OrderId { get; set; }

        /// <summary>
        /// User wallet address - in non-bounceable form.
        /// </summary>
        public string FreelancerAddress { get; set; } = string.Empty;

        [Ignore]
        public User? Freelancer { get; set; }

        public string Text { get; set; } = "- INVALID CELL CONTENT -";

        public decimal Price { get; set; }

        public DateTimeOffset Deadline { get; set; }
    }
}
