namespace InteractiveLeads.Application.Models
{
    /// <summary>
    /// Summary of the tenant's active subscription for access and limit checks.
    /// Includes selected plan price and billing details when available.
    /// </summary>
    public sealed class ActiveSubscriptionInfo
    {
        public Guid SubscriptionId { get; set; }
        public Guid PlanId { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsExpired => EndDate < DateTime.UtcNow.Date;

        /// <summary>Selected billing option. Null for legacy subscriptions.</summary>
        public Guid? PlanPriceId { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        /// <summary>Billing interval: 0 = Month, 1 = Year.</summary>
        public int? BillingInterval { get; set; }
        public int? IntervalCount { get; set; }
    }
}
