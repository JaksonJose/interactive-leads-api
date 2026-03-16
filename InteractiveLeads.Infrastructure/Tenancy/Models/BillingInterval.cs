namespace InteractiveLeads.Infrastructure.Tenancy.Models
{
    /// <summary>
    /// Billing interval for a plan price (e.g. monthly, yearly).
    /// Stored as int in DB; allows future values without code changes.
    /// </summary>
    public enum BillingInterval
    {
        Month = 0,
        Year = 1
    }
}
