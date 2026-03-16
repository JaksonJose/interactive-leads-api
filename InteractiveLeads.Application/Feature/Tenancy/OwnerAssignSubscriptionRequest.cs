namespace InteractiveLeads.Application.Feature.Tenancy
{
    /// <summary>
    /// Request for owner to assign or change their tenant's subscription. TenantId is resolved from the current user context.
    /// </summary>
    public sealed class OwnerAssignSubscriptionRequest
    {
        /// <summary>Billing option (plan price) to assign. Plan is derived from it.</summary>
        public Guid PlanPriceId { get; set; }

        public DateTime EndDate { get; set; }
    }
}
