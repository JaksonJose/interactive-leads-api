namespace InteractiveLeads.Infrastructure.Constants
{
    /// <summary>
    /// Constants for JWT claim types used throughout the application.
    /// </summary>
    /// <remarks>
    /// Defines standard claim names for tenant identification in multi-tenant scenarios.
    /// </remarks>
    public static class ClaimConstants
    {
        /// <summary>
        /// Claim type for identifying the tenant in multi-tenant scenarios.
        /// </summary>
        public const string Tenant = "tenant";
    }
}
