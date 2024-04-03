namespace SomeDAO.Backend.Api
{
    /// <remarks>
    /// <para>
    /// Statuses on smart-contract are not the same as statuses, displayed to the user,
    /// so for display on frontend we need artificial statuses, processed on indexer side.
    /// </para>
    /// <list type="bullet">
    /// <listheader>For customer role:</listheader>
    /// <item>On moderation: status::moderation = 0</item>
    /// <item>No responses: status::active = 1(only those without responses)</item>
    /// <item>Have responses: status::active = 1(only those with responses)</item>
    /// <item>Offer made: status::waiting_freelancer = 2</item>
    /// <item>In the work: status::in_progress = 3</item>
    /// <item>Pending payment: status::fulfilled = 4</item>
    /// <item>Arbitration: status::pre_arbitration = 8, status::on_arbitration = 9</item>
    /// <item>Completed: status::completed = 6, status::refunded = 5, status::payment_forced = 7, status::arbitration_solved = 10</item>
    /// <item>(status::outdated = 11 not displayed for user)</item>
    /// </list>
    /// </remarks>
    public enum CustomerInOrderStatus
    {
        OnModeration = 1,
        NoResponses,
        HaveResponses,
        OfferMade,
        InTheWork,
        PendingPayment,
        Arbitration,
        Completed,
    }
}
