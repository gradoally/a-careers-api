namespace SomeDAO.Backend.Api
{
    /// <remarks>
    /// <para>
    /// Statuses on smart-contract are not the same as statuses, displayed to the user,
    /// so for display on frontend we need artificial statuses, processed on indexer side.
    /// </para>
    /// <list type="bullet">
    /// <listheader>For freelancer role:</listheader>
    /// <item>Response sent: status::active = 1 (only those with responses)</item>
    /// <item>Response denied: status::waiting_freelancer = 2(other user assigned by freelancer)</item>
    /// <item>An offer came in: status::waiting_freelancer = 2(offer accepted)</item>
    /// <item>In the work: status::in_progress = 3</item>
    /// <item>On inspection: status::fulfilled = 4</item>
    /// <item>Arbitration: status::pre_arbitration = 8, status::on_arbitration = 9</item>
    /// <item>Terminated: status::completed = 6, status::refunded = 5, status::payment_forced = 7, status::arbitration_solved = 10</item>
    /// </list>
    /// </remarks>
    public enum FreelancerInOrderStatus
    {
        ResponseSent = 1,
        ResponseDenied,
        AnOfferCameIn,
        InTheWork,
        OnInspection,
        Arbitration,
        Terminated,
    }
}
