namespace InteractiveLeads.Infrastructure.Constants
{
    /// <summary>
    /// Tenant-related constants. Root id/name and passwords are in appsettings (TenancySettings, SysAdminSeedSettings).
    /// </summary>
    public static class TenancyConstants
    {
        /// <summary>
        /// Header name for tenant identification. For configurable value use IOptions&lt;TenancySettings&gt;.TenantIdName.
        /// </summary>
        public const string TenantIdName = "tenant";

        /// <summary>
        /// Identifier used internally for global context (SysAdmin, Support). Not stored in DB; store returns TenantInfo with Id = null.
        /// </summary>
        public const string GlobalTenantIdentifier = "";
    }
}
