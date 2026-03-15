using Microsoft.AspNetCore.Identity;

namespace InteractiveLeads.Infrastructure.Identity.Models
{
    /// <summary>
    /// Represents a claim associated with a role in the application.
    /// Claims define specific permissions or capabilities that can be granted to roles,
    /// providing fine-grained access control throughout the application.
    /// </summary>
    public class ApplicationRoleClaim : IdentityRoleClaim<Guid>
    {
        /// <summary>
        /// Description of what this claim represents and its purpose.
        /// This provides additional context about the claim's functionality
        /// and helps administrators understand what permissions are being granted.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Group or category that this claim belongs to.
        /// This allows for organizing claims into logical groups (e.g., "UserManagement", 
        /// "Reports", "Settings") for better administration and UI organization.
        /// </summary>
        public string Group { get; set; } = string.Empty;
    }
}
