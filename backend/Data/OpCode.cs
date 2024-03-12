namespace SomeDAO.Backend.Data
{
    /// <summary>
    /// Opcodes for Order.
    /// </summary>
    /// <remarks>
    /// Source: <see href="https://github.com/the-real-some-dao/alfa-mater-core/blob/7fb025b6bfc24e4bb7219f6ea65f4184dccaa29e/contracts/constants/op-codes.fc#L3-L16">contracts/constants/op-codes.fc</see>.
    /// </remarks>
    public enum OpCode
    {
        Unknown = 0,

        InitOrder = 15,
        ActivateOrder = 1,
        AddResponse = 2,
        AssignUser = 3,
        AcceptOrder = 30,
        RejectOrder = 31,
        CancelAssign = 32,
        Refund = 44,
        ForcePayment = 45,
        CompleteOrder = 4,
        CustomerFeedback = 5,
        ProcessArbitration = 6,
        SetAdmins = 17,
        OrderCompleted = 20,
        OrderCompletedNotification = 21,
        MasterLog = 239,
    }
}
