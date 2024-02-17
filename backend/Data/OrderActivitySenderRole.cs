using System.ComponentModel;

namespace SomeDAO.Backend.Data
{
    public enum OrderActivitySenderRole
    {
        /// <summary>
        /// Sender is unspecified (usually Master contract).
        /// </summary>
        [Description("Sender is unspecified (usually Master contract)")]
        Unspecified = 0,

        /// <summary>
        /// Sender is customer in this order.
        /// </summary>
        [Description("Sender is customer in this order")]
        Customer = 1,

        /// <summary>
        /// Sender is freelancer in this order.
        /// </summary>
        [Description("Sender is freelancer in this order")]
        Freelancer = 2,
    }
}
