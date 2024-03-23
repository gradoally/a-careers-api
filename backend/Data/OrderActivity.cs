using System.Text.Json.Serialization;
using SQLite;

namespace SomeDAO.Backend.Data
{
    public class OrderActivity
    {
        // https://github.com/the-real-some-dao/a-careers-smc/blob/main/contracts/constants/op-codes.fc#L3-L18
        public const int op_init_order = 15;
        public const int op_activate_order = 1;
        public const int op_add_response = 2;
        public const int op_assign_user = 3;
        public const int op_accept_order = 30;
        public const int op_reject_order = 31;
        public const int op_cancel_assign = 32;
        public const int op_refund = 44;
        public const int op_force_payment = 45;
        public const int op_complete_order = 4;
        public const int op_customer_feedback = 5;
        public const int op_process_arbitration = 6;
        public const int op_set_admins = 17;
        public const int op_order_completed = 20;
        public const int op_order_completed_notification = 21;
        public const int op_master_log = 239;

        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        [JsonIgnore]
        [NotNull, Indexed(Name = "UNQ", Order = 1, Unique = true)]
        public long OrderId { get; set; }

        [Ignore]
        public Order? Order { get; set; }

        [NotNull, Indexed(Name = "UNQ", Order = 2, Unique = true)]
        public long TxLt { get; set; }

        [NotNull]
        public string TxHash { get; set; } = string.Empty;

        [NotNull]
        public DateTimeOffset Timestamp { get; set; }

        [NotNull]
        public int OpCode { get; set; }

        /// <summary>
        /// Role of user who initiated this activity (by OpCode): 1 - customer, 2 - freelancer, 0 - other (admin?).
        /// </summary>
        [Ignore]
        public int SenderRole {
            get
            {
                return OpCode switch
                {
                    op_init_order => 1,
                    op_assign_user => 1,
                    op_cancel_assign => 1,
                    op_refund => 1,
                    op_customer_feedback => 1,

                    op_accept_order => 2,
                    op_reject_order => 2,
                    op_complete_order => 2,
                    op_force_payment => 2,

                    _ => 0,
                };
            }
        }

        /// <summary>
        /// Message sender address.
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
