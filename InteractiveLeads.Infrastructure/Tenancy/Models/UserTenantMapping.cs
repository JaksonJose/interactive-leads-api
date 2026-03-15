using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Tenancy.Models
{
    /// <summary>
    /// Represents a mapping between user emails and their tenant IDs.
    /// This allows efficient tenant resolution without multi-tenant context.
    /// </summary>
    public class UserTenantMapping
    {
        /// <summary>
        /// The user's email address (unique across all tenants).
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the tenant this user belongs to.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Whether this user is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When this mapping was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
