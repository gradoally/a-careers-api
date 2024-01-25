using SQLite;

namespace SomeDAO.Backend.Data
{
	public class User : UserContent
	{
		[PrimaryKey]
		public long Index { get; set; }

		[NotNull, Indexed(Unique = true)]
		public string Address { get; set; } = string.Empty;

		[NotNull, Indexed]
		public string UserAddress { get; set; } = string.Empty;

		public DateTimeOffset? RevokedAt { get; set; }
	}
}
