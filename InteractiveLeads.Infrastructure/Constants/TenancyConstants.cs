namespace InteractiveLeads.Infrastructure.Constants
{
    /// <summary>
    /// Constants for tenant management and multi-tenancy configuration.
    /// </summary>
    /// <remarks>
    /// Defines tenant-related identifiers, default values, and root tenant configuration.
    /// </remarks>
    public class TenancyConstants
    {
        /// <summary>
        /// The parameter name used for tenant identification in requests.
        /// </summary>
        public const string TenantIdName = "tenant";

        /// <summary>
        /// Default password for newly created admin users.
        /// </summary>
        /// <remarks>
        /// Should be changed immediately after first login for security.
        /// </remarks>
        public const string DefaultPassword = "P@ssw0rd@123";

        /// <summary>
        /// Default first name for newly created admin users.
        /// </summary>
        public const string FirstName = "Jakson";

        /// <summary>
        /// Default last name for newly created admin users.
        /// </summary>
        public const string LastName = "Jose";

        /// <summary>
        /// Constants for the root/super admin tenant.
        /// </summary>
        /// <remarks>
        /// The root tenant is used for system-wide administration and
        /// managing all other tenants in the multi-tenant system.
        /// </remarks>
        public static class Root
        {
            /// <summary>
            /// Unique identifier for the root tenant.
            /// </summary>
            public const string Id = "root";

            /// <summary>
            /// Display name for the root tenant.
            /// </summary>
            public const string Name = "root";

            /// <summary>
            /// Email address for the root tenant administrator.
            /// </summary>
            public const string Email = "admin@interactive.com";
        }
    }
}
