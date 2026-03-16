namespace InteractiveLeads.Application.Feature.Tenancy
{
    /// <summary>
    /// Request to assign or change a tenant's subscription to a plan and billing option. Enforces at most one active subscription per tenant.
    /// PlanId is derived from the selected PlanPrice.
    /// </summary>
    public sealed class AssignTenantSubscriptionRequest
    {
        public string TenantId { get; set; } = string.Empty;
        /// <summary>Billing option (plan price) to assign. Plan is derived from it.</summary>
        public Guid PlanPriceId { get; set; }
        public DateTime EndDate { get; set; }
    }
}
