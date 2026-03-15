using Microsoft.AspNetCore.Identity;

namespace InteractiveLeads.Infrastructure.Identity.Models
{
    /// <summary>
    /// Represents a user in the application with extended properties and audit capabilities.
    /// Inherits from IdentityUser to provide authentication and authorization features.
    /// </summary>
    public class ApplicationUser : IdentityUser<Guid>, IAuditableEntity
    {
        /// <summary>
        /// Gets or sets the user's first name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the user's last name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets whether the user account is active.
        /// Inactive users cannot log in to the system.
        /// </summary>
        public bool IsActive { get; set; }
        
        /// <summary>
        /// Gets or sets the tenant ID to which this user belongs.
        /// This establishes the multi-tenant relationship for the user.
        /// Note: This is a reference to the tenant ID in the shared database.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the date and time when the user was created.
        /// This value is automatically set when the user is first saved to the database.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the date and time when the user was last updated.
        /// This value is automatically updated whenever the user is modified and saved.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the user who created this user account.
        /// This value is set when the user is first saved to the database.
        /// Can be null for system-created users or when user context is not available.
        /// </summary>
        public Guid? CreatedBy { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the user who last updated this user account.
        /// This value is updated whenever the user is modified and saved.
        /// Can be null for system-generated updates or when user context is not available.
        /// </summary>
        public Guid? UpdatedBy { get; set; }
        
        /// <summary>
        /// Navigation property for refresh tokens associated with this user.
        /// A user can have multiple refresh tokens for different devices.
        /// </summary>
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
