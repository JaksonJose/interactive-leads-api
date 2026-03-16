namespace InteractiveLeads.Application.Feature.Tenancy
{
    /// <summary>
    /// Request to assign or change a tenant's subscription to a plan. Enforces at most one active subscription per tenant.
    /// </summary>
    public sealed class AssignTenantSubscriptionRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public Guid PlanId { get; set; }
        public DateTime EndDate { get; set; }
    }
}
