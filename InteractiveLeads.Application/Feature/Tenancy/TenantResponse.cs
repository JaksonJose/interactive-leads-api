using InteractiveLeads.Application.Feature.Users;

namespace InteractiveLeads.Application.Feature.Tenancy
{
    /// <summary>
    /// Response model containing tenant information.
    /// </summary>
    /// <remarks>
    /// Returned after tenant operations such as retrieval, creation, or updates.
    /// Contains all tenant details including identification, contact, and subscription information.
    /// </remarks>
    public sealed class TenantResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier for the tenant.
        /// </summary>
        public string Identifier { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the tenant organization.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the database connection string for the tenant's isolated data.
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the email address of the tenant's primary administrator.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the first name of the tenant's primary administrator.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last name of the tenant's primary administrator.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expiration date of the tenant's subscription.
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the tenant is active.
        /// </summary>
        public bool IsActive { get; set; }

        public UserResponse User { get; set; } = default!;
    }
}
