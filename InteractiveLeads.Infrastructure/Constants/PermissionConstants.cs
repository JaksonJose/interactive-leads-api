using System.Collections.ObjectModel;

namespace InteractiveLeads.Infrastructure.Constants
{
    /// <summary>
    /// Constants defining available actions for permission-based authorization.
    /// </summary>
    /// <remarks>
    /// Used in combination with InteractiveFeature to create granular permissions.
    /// </remarks>
    public static class InteractiveAction
    {
        /// <summary>
        /// Permission action for reading/viewing resources.
        /// </summary>
        public const string Read = nameof(Read);

        /// <summary>
        /// Permission action for creating new resources.
        /// </summary>
        public const string Create = nameof(Create);

        /// <summary>
        /// Permission action for updating existing resources.
        /// </summary>
        public const string Update = nameof(Update);

        /// <summary>
        /// Permission action for deleting resources.
        /// </summary>
        public const string Delete = nameof(Delete);

        /// <summary>
        /// Permission action for refreshing authentication tokens.
        /// </summary>
        public const string RefreshToken = nameof(RefreshToken);

    /// <summary>
    /// Permission action for upgrading tenant subscriptions.
    /// </summary>
    public const string UpgradeSubscription = nameof(UpgradeSubscription);
    }

    /// <summary>
    /// Constants defining permission types for cross-tenant operations.
    /// </summary>
    /// <remarks>
    /// Used to categorize permissions by their scope and access level.
    /// </remarks>
    public static class InteractivePermissionType
    {
        /// <summary>
        /// Permission for operations within the current tenant only.
        /// </summary>
        public const string Tenant = nameof(Tenant);

        /// <summary>
        /// Permission for cross-tenant operations with limited access.
        /// </summary>
        public const string CrossTenant = nameof(CrossTenant);

        /// <summary>
        /// Permission for system-wide operations with full access.
        /// </summary>
        public const string System = nameof(System);
    }

    /// <summary>
    /// Constants defining application features for permission-based authorization.
    /// </summary>
    /// <remarks>
    /// Used in combination with InteractiveAction to create specific permissions.
    /// </remarks>
    public static class InteractiveFeature
    {
        /// <summary>
        /// Feature identifier for tenant management operations.
        /// </summary>
        public const string Tenants = nameof(Tenants);

        /// <summary>
        /// Feature identifier for user management operations.
        /// </summary>
        public const string Users = nameof(Users);

        /// <summary>
        /// Feature identifier for role management operations.
        /// </summary>
        public const string Roles = nameof(Roles);

        /// <summary>
        /// Feature identifier for user-role assignment operations.
        /// </summary>
        public const string UserRoles = nameof(UserRoles);

        /// <summary>
        /// Feature identifier for role claim/permission operations.
        /// </summary>
        public const string RoleClaims = nameof(RoleClaims);

    /// <summary>
    /// Feature identifier for token/authentication operations.
    /// </summary>
    public const string Tokens = nameof(Tokens);

    /// <summary>
    /// Feature identifier for cross-tenant user management operations.
    /// </summary>
    public const string CrossTenantUsers = nameof(CrossTenantUsers);

    /// <summary>
    /// Feature identifier for cross-tenant role management operations.
    /// </summary>
    public const string CrossTenantRoles = nameof(CrossTenantRoles);

    /// <summary>
    /// Feature identifier for cross-tenant tenant management operations.
    /// </summary>
    public const string CrossTenantTenants = nameof(CrossTenantTenants);

    /// <summary>
    /// Feature identifier for system logs operations.
    /// </summary>
    public const string SystemLogs = nameof(SystemLogs);

    /// <summary>
    /// Feature identifier for system monitoring operations.
    /// </summary>
    public const string SystemMonitoring = nameof(SystemMonitoring);

    /// <summary>
    /// Feature identifier for system configuration operations.
    /// </summary>
    public const string SystemConfiguration = nameof(SystemConfiguration);
    }

    /// <summary>
    /// Record representing a single permission in the system.
    /// </summary>
    /// <param name="Action">The action type (e.g., Create, Read, Update, Delete).</param>
    /// <param name="Feature">The feature this permission applies to.</param>
    /// <param name="Description">Human-readable description of the permission.</param>
    /// <param name="group">The permission group for organizational purposes.</param>
    /// <param name="IsBasic">Indicates if this permission is granted to basic users.</param>
    /// <param name="IsRoot">Indicates if this permission is reserved for root administrators.</param>
    /// <param name="PermissionType">The type of permission (Tenant, CrossTenant, or System).</param>
    /// <param name="AllowedTenants">Specific tenant IDs this permission applies to (null means all tenants).</param>
    /// <remarks>
    /// Permissions are composed of an action and a feature, creating a unique permission name.
    /// </remarks>
    public record InteractivePermission(string Action, string Feature, string Description, string group, bool IsBasic = false, bool IsRoot = false, string PermissionType = InteractivePermissionType.Tenant, string[]? AllowedTenants = null)
    {
        /// <summary>
        /// Gets the fully qualified name of the permission.
        /// </summary>
        public string Name => NameFor(Action, Feature);

        /// <summary>
        /// Creates a permission name from an action and feature.
        /// </summary>
        /// <param name="action">The action type.</param>
        /// <param name="feature">The feature name.</param>
        /// <returns>A formatted permission name.</returns>
        public static string NameFor(string action, string feature) => $"Permission.{feature}.{action}";
    }

    /// <summary>
    /// Central repository of all system permissions organized by role level.
    /// </summary>
    /// <remarks>
    /// Defines the complete permission structure for the application including:
    /// - All available permissions
    /// - Cross-tenant permissions (SysAdmin, Support)
    /// - Tenant-specific permissions (Owner, Manager, Agent)
    /// - Legacy permissions for backward compatibility
    /// </remarks>
    public static class InteractivePermissions
    {
        private static readonly InteractivePermission[] _allPermissions = 
        [
            // System/Tenant Management Permissions (SysAdmin only)
            new InteractivePermission(InteractiveAction.Create, InteractiveFeature.Tenants, "Create Tenants", "Tenancy", IsRoot: true, PermissionType: InteractivePermissionType.System),
            new InteractivePermission(InteractiveAction.Read, InteractiveFeature.Tenants, "Read Tenants", "Tenancy", IsRoot: true, PermissionType: InteractivePermissionType.System),
            new InteractivePermission(InteractiveAction.Update, InteractiveFeature.Tenants, "Update Tenants", "Tenancy", IsRoot: true, PermissionType: InteractivePermissionType.System),
            new InteractivePermission(InteractiveAction.Delete, InteractiveFeature.Tenants, "Delete Tenants", "Tenancy", IsRoot: true, PermissionType: InteractivePermissionType.System),
            new InteractivePermission(InteractiveAction.UpgradeSubscription, InteractiveFeature.Tenants, "Upgrade Tenant Subscription", "Tenancy", IsRoot: true, PermissionType: InteractivePermissionType.System),

            // User Management Permissions (Tenant-level)
            new InteractivePermission(InteractiveAction.Create, InteractiveFeature.Users, "Create Users", "UserManagement"),
            new InteractivePermission(InteractiveAction.Read, InteractiveFeature.Users, "Read Users", "UserManagement"),
            new InteractivePermission(InteractiveAction.Update, InteractiveFeature.Users, "Update Users", "UserManagement"),
            new InteractivePermission(InteractiveAction.Delete, InteractiveFeature.Users, "Delete Users", "UserManagement"),

            // Role Management Permissions (Tenant-level)
            new InteractivePermission(InteractiveAction.Create, InteractiveFeature.Roles, "Create Roles", "RoleManagement"),
            new InteractivePermission(InteractiveAction.Read, InteractiveFeature.Roles, "Read Roles", "RoleManagement"),
            new InteractivePermission(InteractiveAction.Update, InteractiveFeature.Roles, "Update Roles", "RoleManagement"),
            new InteractivePermission(InteractiveAction.Delete, InteractiveFeature.Roles, "Delete Roles", "RoleManagement"),

            // User-Role Assignment Permissions (Tenant-level)
            new InteractivePermission(InteractiveAction.Read, InteractiveFeature.UserRoles, "Read User Roles", "UserManagement"),
            new InteractivePermission(InteractiveAction.Update, InteractiveFeature.UserRoles, "Update User Roles", "UserManagement"),

            // Role Claims/Permissions Management (Tenant-level)
            new InteractivePermission(InteractiveAction.Read, InteractiveFeature.RoleClaims, "Read Role Claims/Permissions", "RoleManagement"),
            new InteractivePermission(InteractiveAction.Update, InteractiveFeature.RoleClaims, "Update Role Claims/Permissions", "RoleManagement"),

            // Token Management (Basic access)
            new InteractivePermission(InteractiveAction.RefreshToken, InteractiveFeature.Tokens, "Generate Refresh Token", "Authentication", IsBasic: true),
            
            // Cross-Tenant Permissions for SysAdmin (System level)
            new InteractivePermission(InteractiveAction.Read, InteractiveFeature.CrossTenantUsers, "Read Users Across All Tenants", "CrossTenant", PermissionType: InteractivePermissionType.System),
            new InteractivePermission(InteractiveAction.Create, InteractiveFeature.CrossTenantUsers, "Create Users in Any Tenant", "CrossTenant", PermissionType: InteractivePermissionType.System),
            new InteractivePermission(InteractiveAction.Update, InteractiveFeature.CrossTenantUsers, "Update Users in Any Tenant", "CrossTenant", PermissionType: InteractivePermissionType.System),
            new InteractivePermission(InteractiveAction.Delete, InteractiveFeature.CrossTenantUsers, "Delete Users in Any Tenant", "CrossTenant", PermissionType: InteractivePermissionType.System),
            
            new InteractivePermission(InteractiveAction.Read, InteractiveFeature.CrossTenantRoles, "Read Roles Across All Tenants", "CrossTenant", PermissionType: InteractivePermissionType.System),
            new InteractivePermission(InteractiveAction.Update, InteractiveFeature.CrossTenantRoles, "Update Roles Across All Tenants", "CrossTenant", PermissionType: InteractivePermissionType.System),
            
            new InteractivePermission(InteractiveAction.Create, InteractiveFeature.CrossTenantTenants, "Create Tenants", "CrossTenant", PermissionType: InteractivePermissionType.System),
            new InteractivePermission(InteractiveAction.Read, InteractiveFeature.CrossTenantTenants, "Read All Tenants", "CrossTenant", PermissionType: InteractivePermissionType.System),
            new InteractivePermission(InteractiveAction.Update, InteractiveFeature.CrossTenantTenants, "Update Tenants", "CrossTenant", PermissionType: InteractivePermissionType.System),
            new InteractivePermission(InteractiveAction.Delete, InteractiveFeature.CrossTenantTenants, "Delete Tenants", "CrossTenant", PermissionType: InteractivePermissionType.System),
            
            new InteractivePermission(InteractiveAction.Read, InteractiveFeature.SystemLogs, "Read System Logs", "SystemManagement", PermissionType: InteractivePermissionType.System),
            new InteractivePermission(InteractiveAction.Read, InteractiveFeature.SystemMonitoring, "Read System Monitoring", "SystemManagement", PermissionType: InteractivePermissionType.System),
            new InteractivePermission(InteractiveAction.Update, InteractiveFeature.SystemConfiguration, "Update System Configuration", "SystemManagement", PermissionType: InteractivePermissionType.System),

            // Cross-Tenant Permissions for Support (CrossTenant level)
            new InteractivePermission(InteractiveAction.Read, InteractiveFeature.CrossTenantUsers, "Read Users Across All Tenants", "CrossTenant", PermissionType: InteractivePermissionType.CrossTenant),
            new InteractivePermission(InteractiveAction.Update, InteractiveFeature.CrossTenantUsers, "Update Users in Any Tenant", "CrossTenant", PermissionType: InteractivePermissionType.CrossTenant),
            
            new InteractivePermission(InteractiveAction.Read, InteractiveFeature.CrossTenantRoles, "Read Roles Across All Tenants", "CrossTenant", PermissionType: InteractivePermissionType.CrossTenant),
            
            new InteractivePermission(InteractiveAction.Read, InteractiveFeature.CrossTenantTenants, "Read All Tenants", "CrossTenant", PermissionType: InteractivePermissionType.CrossTenant),
            
            new InteractivePermission(InteractiveAction.Read, InteractiveFeature.SystemLogs, "Read System Logs", "SystemManagement", PermissionType: InteractivePermissionType.CrossTenant),
            new InteractivePermission(InteractiveAction.Read, InteractiveFeature.SystemMonitoring, "Read System Monitoring", "SystemManagement", PermissionType: InteractivePermissionType.CrossTenant),
        ];

        /// <summary>
        /// Gets all permissions available in the system.
        /// </summary>
        public static IReadOnlyList<InteractivePermission> All { get; } = new ReadOnlyCollection<InteractivePermission>(_allPermissions);

        /// <summary>
        /// Gets system administrator permissions for full cross-tenant access.
        /// </summary>
        /// <remarks>
        /// SysAdmin has full access to all tenants and system operations.
        /// </remarks>
        public static IReadOnlyList<InteractivePermission> SysAdmin { get; } = new ReadOnlyCollection<InteractivePermission>([.. _allPermissions.Where(p => 
            p.PermissionType == InteractivePermissionType.System || p.IsRoot
        )]);
        
        /// <summary>
        /// Gets support user permissions for cross-tenant support operations.
        /// </summary>
        /// <remarks>
        /// Support has limited cross-tenant access for customer support operations.
        /// </remarks>
        public static IReadOnlyList<InteractivePermission> Support { get; } = new ReadOnlyCollection<InteractivePermission>([.. _allPermissions.Where(p => 
            p.PermissionType == InteractivePermissionType.CrossTenant
        )]);

        /// <summary>
        /// Gets tenant owner permissions for full control within their tenant.
        /// </summary>
        /// <remarks>
        /// Owner has full control over their tenant including user and role management.
        /// </remarks>
        public static IReadOnlyList<InteractivePermission> Owner { get; } = new ReadOnlyCollection<InteractivePermission>([.. _allPermissions.Where(p => 
            p.PermissionType == InteractivePermissionType.Tenant && 
            (p.Feature == InteractiveFeature.Users || p.Feature == InteractiveFeature.Roles || 
             p.Feature == InteractiveFeature.UserRoles || p.Feature == InteractiveFeature.RoleClaims ||
             p.Feature == InteractiveFeature.Tokens)
        )]);

        /// <summary>
        /// Gets tenant manager permissions for user management within their tenant.
        /// </summary>
        /// <remarks>
        /// Manager can manage users and roles but cannot modify tenant configuration.
        /// </remarks>
        public static IReadOnlyList<InteractivePermission> Manager { get; } = new ReadOnlyCollection<InteractivePermission>([.. _allPermissions.Where(p => 
            p.PermissionType == InteractivePermissionType.Tenant && 
            (p.Feature == InteractiveFeature.Users || p.Feature == InteractiveFeature.UserRoles ||
             p.Feature == InteractiveFeature.Tokens) &&
            (p.Action == InteractiveAction.Read || p.Action == InteractiveAction.Update)
        )]);

        /// <summary>
        /// Gets tenant agent permissions for basic operations within their tenant.
        /// </summary>
        /// <remarks>
        /// Agent has basic read access and can perform standard operations.
        /// </remarks>
        public static IReadOnlyList<InteractivePermission> Agent { get; } = new ReadOnlyCollection<InteractivePermission>([.. _allPermissions.Where(p => 
            p.PermissionType == InteractivePermissionType.Tenant && 
            p.Action == InteractiveAction.Read && 
            (p.Feature == InteractiveFeature.Users || p.Feature == InteractiveFeature.Tokens)
        )]);

        // Legacy role permissions for backward compatibility
        /// <summary>
        /// Gets legacy admin permissions (same as Owner).
        /// </summary>
        /// <remarks>
        /// @deprecated Use Owner instead.
        /// </remarks>
        [Obsolete("Use Owner instead")]
        public static IReadOnlyList<InteractivePermission> Admin { get; } = Owner;
        
        /// <summary>
        /// Gets legacy basic permissions (same as Agent).
        /// </summary>
        /// <remarks>
        /// @deprecated Use Agent instead.
        /// </remarks>
        [Obsolete("Use Agent instead")]
        public static IReadOnlyList<InteractivePermission> Basic { get; } = Agent;

        /// <summary>
        /// Gets root/super administrator permissions for system-wide tenant management.
        /// </summary>
        /// <remarks>
        /// @deprecated Use SysAdmin instead.
        /// </remarks>
        [Obsolete("Use SysAdmin instead")]
        public static IReadOnlyList<InteractivePermission> Root { get; } = SysAdmin;

        /// <summary>
        /// Gets permissions for a specific role.
        /// </summary>
        /// <param name="roleName">The role name.</param>
        /// <returns>List of permissions for the role.</returns>
        public static IReadOnlyList<InteractivePermission> GetPermissionsForRole(string roleName) => roleName switch
        {
            RoleConstants.SysAdmin => SysAdmin,
            RoleConstants.Support => Support,
            RoleConstants.Owner => Owner,
            RoleConstants.Manager => Manager,
            RoleConstants.Agent => Agent,
            _ => new ReadOnlyCollection<InteractivePermission>(Array.Empty<InteractivePermission>())
        };
    }
}
