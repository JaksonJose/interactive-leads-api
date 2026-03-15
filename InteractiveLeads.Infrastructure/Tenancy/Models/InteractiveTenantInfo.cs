using Finbuckle.MultiTenant.Abstractions;

namespace InteractiveLeads.Infrastructure.Tenancy.Models
{
    /// <summary>
    /// Represents a tenant in the application.
    /// Implements ITenantInfo for compatibility with Finbuckle.MultiTenant.
    /// </summary>
    public sealed class InteractiveTenantInfo : ITenantInfo
    {
        /// <summary>
        /// The tenant identifier required by Finbuckle.
        /// Stored as string internally but can be represented as Guid via TenantGuid.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// The unique identifier for the tenant (slug or code).
        /// Used for tenant resolution and routing.
        /// </summary>
        public string? Identifier { get; set; }

        /// <summary>
        /// The tenant's display name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The contact email for the tenant. Use to login.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The tenant owner's first name.
        /// Optional.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// The tenant owner's last name.
        /// Optional.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the tenant is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// The date until which the tenant is valid.
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// The database connection string for the tenant.
        /// Optional - supports hybrid architecture where tenants can share database.
        /// </summary>
        public string? ConnectionString { get; set; }
    }
}
