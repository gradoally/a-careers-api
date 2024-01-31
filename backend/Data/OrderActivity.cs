using SQLite;

namespace SomeDAO.Backend.Data
{
	public class OrderActivity
	{
		[PrimaryKey, AutoIncrement]
		public long Id { get; set; }

		[NotNull, Indexed(Name = "UNQ", Order = 1, Unique = true)]
		public long OrderId { get; set; }

        [Ignore]
        public Order? Order { get; set; }

		[NotNull, Indexed(Name = "UNQ", Order = 2, Unique = true)]
		public long TxLt { get;set; }

		[NotNull]
		public string TxHash { get; set; } = string.Empty;

		[NotNull]
		public DateTimeOffset Timestamp { get; set; }

		[NotNull]
		public OpCode OpCode { get; set; }

		[NotNull]
		public OrderActivitySenderRole SenderRole { get; set; }

		/// <summary>
		/// Message sender address: for customer or freelancer - in non-bounceable form, for others - in bounceable form.
		/// </summary>
        /// <remarks>
        /// See also: <seealso cref="SenderRole"/>.
        /// </remarks>
		[NotNull, Indexed]
		public string SenderAddress { get; set; } = string.Empty;

        [Ignore]
        public User? Sender { get; set; }

		public decimal? Amount { get; set; }
	}
}
