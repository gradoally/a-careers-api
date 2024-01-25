using SQLite;

namespace SomeDAO.Backend.Data
{
	public class Admin : AdminContent
	{
		[PrimaryKey]
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
	}
}
