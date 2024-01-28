using System.Text.Json.Serialization;
using SQLite;

namespace SomeDAO.Backend.Data
{
    public class Order : IOrderContent
    {
		[PrimaryKey]
		public long Index { get; set; }

		/// <summary>
		/// Smartcontract address - in bounceable form.
		/// </summary>
		[NotNull, Indexed(Unique = true)]
		public string Address { get; set; } = string.Empty;

        [Indexed]
        public OrderStatus Status { get; set; }

		/// <summary>
		/// User wallet address - in non-bounceable form.
		/// </summary>
		[NotNull, Indexed]
		public string CustomerAddress { get; set; } = string.Empty;

		[Ignore]
		public User? Customer { get; set; }

		/// <summary>
		/// User wallet address - in non-bounceable form.
		/// </summary>
		[Indexed]
		public string? FreelancerAddress { get; set; }

		[Ignore]
		public User? Freelancer { get; set; }

		public DateTimeOffset CreatedAt { get; set; }

		public int ResponsesCount { get; set; }

		#region IOrderContent

		public string? Category { get; set; }

		public string? Language { get; set; }

		public string? Name { get; set; }

		public decimal Price { get; set; }

		public DateTimeOffset Deadline { get; set; }

		public string? Description { get; set; }

		public string? TechnicalTask { get; set; }

		#endregion

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
