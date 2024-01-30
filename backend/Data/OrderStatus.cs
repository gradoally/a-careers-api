namespace SomeDAO.Backend.Data
{
    /// <summary>
    /// Order statuses.
    /// </summary>
    /// <remarks>
    /// Source: <seealso href="https://github.com/the-real-some-dao/alfa-mater-core/blob/main/contracts/constants/constants.fc#L13C1-L24C33">constants.fc</seealso>.
    /// </remarks>
    public enum OrderStatus
    {
        Moderation = 0,
        Active = 1,
        WaitingFreelancer = 2,
        InProgress = 3,
        Fulfilled = 4,
        Refunded = 5,
        Completed = 6,
        PaymentForced = 7,
        PreArbitration = 8,
        OnArbitration = 9,
        ArbitrationSolved = 10,
        Outdated = 11,
    }
}
