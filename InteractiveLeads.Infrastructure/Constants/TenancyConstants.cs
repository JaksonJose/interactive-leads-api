namespace InteractiveLeads.Infrastructure.Constants
{
    /// <summary>
    /// Tenant-related constant used only where DI is not available (e.g. attributes).
    /// Root id/name and passwords are in appsettings (TenancySettings, SysAdminSeedSettings).
    /// </summary>
    public static class TenancyConstants
    {
        /// <summary>
        /// Header name for tenant identification. For configurable value use IOptions&lt;TenancySettings&gt;.TenantIdName.
        /// </summary>
        public const string TenantIdName = "tenant";
    }
}
