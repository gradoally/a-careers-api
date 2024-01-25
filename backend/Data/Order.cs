using SQLite;

namespace SomeDAO.Backend.Data
{
    public class Order : OrderContent
    {
		[PrimaryKey]
		public long Index { get; set; }

		[NotNull, Indexed(Unique = true)]
		public string Address { get; set; } = string.Empty;

        [NotNull, Indexed]
        public int Status { get; set; }

		[NotNull, Indexed]
		public string CustomerAddress { get; set; } = string.Empty;

		[Indexed]
		public string? FreelancerAddress { get; set; } = string.Empty;
    }
}
