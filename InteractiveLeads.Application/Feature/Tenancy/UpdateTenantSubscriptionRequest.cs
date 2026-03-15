namespace InteractiveLeads.Application.Feature.Tenancy
{
    /// <summary>
    /// Request model for updating a tenant's subscription information.
    /// </summary>
    /// <remarks>
    /// Used to extend or modify the expiration date of a tenant's subscription.
    /// </remarks>
    public class UpdateTenantSubscriptionRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier of the tenant whose subscription is being updated.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new expiration date for the tenant's subscription.
        /// </summary>
        public DateTime NewExpirationDate { get; set; }
    }
}
