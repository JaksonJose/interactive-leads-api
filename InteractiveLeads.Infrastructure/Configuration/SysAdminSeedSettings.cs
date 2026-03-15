namespace InteractiveLeads.Infrastructure.Configuration
{
    /// <summary>
    /// Single configuration for root/SysAdmin: credentials to access the application and create other users.
    /// Store in appsettings (or user secrets / environment); do not commit sensitive values.
    /// </summary>
    public class SysAdminSeedSettings
    {
        public const string SectionName = "SysAdminSeed";

        /// <summary>Root tenant identifier (e.g. "root").</summary>
        public string RootId { get; set; } = "root";

        /// <summary>Root tenant display name.</summary>
        public string RootName { get; set; } = "root";

        /// <summary>Email for the root administrator (login).</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Password for the root administrator. Change after first login.</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>First name of the root administrator.</summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>Last name of the root administrator.</summary>
        public string LastName { get; set; } = string.Empty;
    }
}
