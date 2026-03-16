namespace InteractiveLeads.Application.Models
{
    /// <summary>
    /// Summary of the tenant's active subscription for access and limit checks.
    /// </summary>
    public sealed class ActiveSubscriptionInfo
    {
        public Guid SubscriptionId { get; set; }
        public Guid PlanId { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsExpired => EndDate < DateTime.UtcNow.Date;
    }
}
