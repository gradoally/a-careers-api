using System.Text.Json.Serialization;
using SQLite;

namespace SomeDAO.Backend.Data
{
    public class Order : OrderContent
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
