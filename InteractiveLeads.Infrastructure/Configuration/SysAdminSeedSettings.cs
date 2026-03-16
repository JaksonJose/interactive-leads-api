namespace InteractiveLeads.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration for global SysAdmin: credentials to access the application and create other users.
    /// Store in appsettings (or user secrets / environment); do not commit sensitive values.
    /// </summary>
    public class SysAdminSeedSettings
    {
        public const string SectionName = "SysAdminSeed";

        /// <summary>Email for the global SysAdmin (login).</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Password for the global SysAdmin. Change after first login.</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>First name of the global SysAdmin.</summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>Last name of the global SysAdmin.</summary>
        public string LastName { get; set; } = string.Empty;

        // DefaultTenantOwnerPassword is no longer used. Tenant owner is now created inactive with a temporary password and must activate via invitation link.
    }
}
