namespace InteractiveLeads.Infrastructure.Tenancy.Models
{
    /// <summary>
    /// Status of a tenant subscription. At most one Active subscription per tenant.
    /// </summary>
    public enum SubscriptionStatus
    {
        Active = 0,
        Cancelled = 1,
        Expired = 2,
        Trial = 3
    }
}
