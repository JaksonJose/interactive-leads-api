namespace InteractiveLeads.Infrastructure.Tenancy.Models
{
    /// <summary>
    /// Links a tenant to a plan. At most one active subscription per tenant.
    /// ExternalId reserved for future payment gateway integration.
    /// </summary>
    public sealed class Subscription
    {
        public Guid Id { get; set; }
        /// <summary>References Tenants.Id in the same tenant store.</summary>
        public string TenantId { get; set; } = string.Empty;
        public Guid PlanId { get; set; }
        public Plan Plan { get; set; } = null!;
        /// <summary>Which billing option the tenant selected. Null for legacy subscriptions.</summary>
        public Guid? PlanPriceId { get; set; }
        public PlanPrice? PlanPrice { get; set; }
        public SubscriptionStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        /// <summary>Reserved for payment gateway external reference.</summary>
        public string? ExternalId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
