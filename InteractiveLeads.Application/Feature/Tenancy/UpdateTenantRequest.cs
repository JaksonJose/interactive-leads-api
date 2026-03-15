namespace InteractiveLeads.Application.Feature.Tenancy
{
    /// <summary>
    /// Request model for updating an existing tenant in the multi-tenant system.
    /// </summary>
    /// <remarks>
    /// Contains all necessary information to update a tenant including
    /// identification details, database connection, and admin user information.
    /// </remarks>
    public class UpdateTenantRequest
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
        /// Optional - null for shared database scenarios in hybrid architecture.
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
    }
}

