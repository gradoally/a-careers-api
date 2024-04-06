using System.Text.Json.Serialization;
using SQLite;

namespace SomeDAO.Backend.Data
{
    public class Order : IOrderContent, IBlockchainEntity
    {
        // https://github.com/the-real-some-dao/a-careers-smc/blob/main/contracts/constants/constants.fc#L14-L23
        public const int status_active = 1;
        public const int status_waiting_freelancer = 2;
        public const int status_in_progress = 3;
        public const int status_fulfilled = 4;
        public const int status_refunded = 5;
        public const int status_completed = 6;
        public const int status_payment_forced = 7;
        public const int status_arbitration_solved = 10;

        [JsonIgnore]
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        [NotNull, Indexed(Unique = true)]
        public long Index { get; set; }

        /// <summary>
        /// Smartcontract address - in bounceable form.
        /// </summary>
        [NotNull, Indexed(Unique = true)]
        public string Address { get; set; } = string.Empty;

        [Indexed]
        public int Status { get; set; }

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

        public string? Description { get; set; }

        public string? TechnicalTask { get; set; }

        #endregion

        public decimal Price { get; set; }

        public DateTimeOffset Deadline { get; set; }

        public int TimeForCheck { get; set; }

        public DateTimeOffset CompletedAt { get; set; }

        public int ArbitrationFreelancerPart { get; set; }

        public string? Result { get; set; }

        /// <remarks>
        /// Copy of <see href="https://github.com/the-real-some-dao/a-careers-smc/blob/main/contracts/order.fc#L452-L465">(int) get_force_payment_availability() method_id</see>.
        /// </remarks>
        [Ignore]
        public bool ForcePaymentAvailable
        {
            get
            {
                return Status == status_fulfilled
                    && DateTimeOffset.UtcNow > CompletedAt.AddSeconds(TimeForCheck);
            }
        }

        /// <remarks>
        /// Copy of <see href="https://github.com/the-real-some-dao/a-careers-smc/blob/main/contracts/order.fc#L467-L480">(int) get_refund_availability() method_id</see>.
        /// </remarks>
        [Ignore]
        public bool RefundAvailable
        {
            get
            {
                return Status == status_in_progress
                    && DateTimeOffset.UtcNow > Deadline;
            }
        }

        #region IBlockchainEntity

        [JsonIgnore]
        public EntityType EntityType { get; } = EntityType.Order;

        [JsonIgnore]
        public long LastTxLt { get; set; }

        [JsonIgnore]
        public string? LastTxHash { get; set; }

        [JsonIgnore]
        public DateTimeOffset LastSync { get; set; }

        #endregion

        [JsonIgnore]
        public byte[]? NameHash { get; set; }

        [Ignore]
        public string? NameTranslated { get; set; }

        [JsonIgnore]
        public byte[]? DescriptionHash { get; set; }

        [Ignore]
        public string? DescriptionTranslated { get; set; }

        [JsonIgnore]
        public byte[]? TechnicalTaskHash { get; set; }

        [Ignore]
        public string? TechnicalTaskTranslated { get; set; }

        [Ignore]
        public OrderResponse? CurrentUserResponse { get; set; }

        [JsonIgnore]
        public bool NeedTranslation { get; set; }

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

        public Order ShallowCopy()
        {
            return (Order)MemberwiseClone();
        }
    }
}
