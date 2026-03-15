using Microsoft.AspNetCore.Identity;

namespace InteractiveLeads.Infrastructure.Identity.Models
{
    /// <summary>
    /// Represents a role in the application with extended properties and audit capabilities.
    /// Inherits from IdentityRole to provide authorization features.
    /// </summary>
    public class ApplicationRole : IdentityRole<Guid>
    {
        /// <summary>
        /// Description of the role.
        /// This provides additional context about what the role is used for.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// The date and time when the role was created.
        /// This value is automatically set when the role is first saved to the database.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// The date and time when the role was last updated.
        /// This value is automatically updated whenever the role is modified and saved.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
        
        /// <summary>
        /// The ID of the user who created this role.
        /// This value is set when the role is first saved to the database.
        /// Can be null for system-created roles or when user context is not available.
        /// </summary>
        public Guid? CreatedBy { get; set; }
        
        /// <summary>
        /// The ID of the user who last updated this role.
        /// This value is updated whenever the role is modified and saved.
        /// Can be null for system-generated updates or when user context is not available.
        /// </summary>
        public Guid? UpdatedBy { get; set; }

        /// <summary>
        /// Navigation property for role claims.
        /// This provides access to all claims associated with this role.
        /// </summary>
        public virtual ICollection<ApplicationRoleClaim> Claims { get; set; } = new List<ApplicationRoleClaim>();
    }
}
