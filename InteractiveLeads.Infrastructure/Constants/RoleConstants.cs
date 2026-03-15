using System.Collections.ObjectModel;

namespace InteractiveLeads.Infrastructure.Constants
{
    /// <summary>
    /// Constants for system role names and role management.
    /// </summary>
    /// <remarks>
    /// Defines all available roles in the system including cross-tenant and tenant-specific roles.
    /// </remarks>
    public static class RoleConstants
    {
        // Cross-Tenant Roles (System-wide access)
        /// <summary>
        /// Role name for system administrators with full cross-tenant access.
        /// Can manage all tenants, users, and system configuration.
        /// </summary>
        public const string SysAdmin = nameof(SysAdmin);

        /// <summary>
        /// Role name for support users with limited cross-tenant access.
        /// Can read and update users across tenants but cannot create/delete.
        /// </summary>
        public const string Support = nameof(Support);

        // Tenant-Specific Roles
        /// <summary>
        /// Role name for tenant owners with full control over their tenant.
        /// Can manage all users, roles, and settings within their tenant.
        /// </summary>
        public const string Owner = nameof(Owner);

        /// <summary>
        /// Role name for tenant managers with elevated permissions within their tenant.
        /// Can manage users and most settings but cannot modify tenant configuration.
        /// </summary>
        public const string Manager = nameof(Manager);

        /// <summary>
        /// Role name for basic agents with standard user permissions.
        /// Can perform basic operations within their tenant.
        /// </summary>
        public const string Agent = nameof(Agent);

        /// <summary>
        /// Gets the list of cross-tenant roles that can access multiple tenants.
        /// </summary>
        public static IReadOnlyList<string> CrossTenantRoles { get; } = new ReadOnlyCollection<string>([SysAdmin, Support]);

        /// <summary>
        /// Gets the list of system-wide roles that have elevated permissions.
        /// </summary>
        public static IReadOnlyList<string> SystemRoles { get; } = new ReadOnlyCollection<string>([SysAdmin, Support]);

        /// <summary>
        /// Gets the list of tenant-specific roles for regular tenant operations.
        /// </summary>
        public static IReadOnlyList<string> TenantRoles { get; } = new ReadOnlyCollection<string>([Owner, Manager, Agent]);

        /// <summary>
        /// Gets the list of all available roles in the system.
        /// </summary>
        public static IReadOnlyList<string> AllRoles { get; } = new ReadOnlyCollection<string>([SysAdmin, Support, Owner, Manager, Agent]);

        /// <summary>
        /// Gets the list of default roles created for each tenant.
        /// </summary>
        public static IReadOnlyList<string> DefaultTenantRoles { get; } = new ReadOnlyCollection<string>([Owner, Manager, Agent]);

        /// <summary>
        /// Determines whether the specified role name is a cross-tenant role.
        /// </summary>
        /// <param name="roleName">The role name to check.</param>
        /// <returns>True if the role is a cross-tenant role, otherwise false.</returns>
        public static bool IsCrossTenantRole(string roleName) => CrossTenantRoles.Contains(roleName);

        /// <summary>
        /// Determines whether the specified role name is a system-wide role.
        /// </summary>
        /// <param name="roleName">The role name to check.</param>
        /// <returns>True if the role is a system role, otherwise false.</returns>
        public static bool IsSystemRole(string roleName) => SystemRoles.Contains(roleName);

        /// <summary>
        /// Determines whether the specified role name is a tenant-specific role.
        /// </summary>
        /// <param name="roleName">The role name to check.</param>
        /// <returns>True if the role is a tenant role, otherwise false.</returns>
        public static bool IsTenantRole(string roleName) => TenantRoles.Contains(roleName);

        /// <summary>
        /// Determines whether the specified role name is a default role.
        /// </summary>
        /// <param name="roleName">The role name to check.</param>
        /// <returns>True if the role is a default role, otherwise false.</returns>
        public static bool IsDefaultRole(string roleName) => DefaultTenantRoles.Contains(roleName);

        /// <summary>
        /// Gets the role hierarchy level for permission inheritance.
        /// Higher numbers indicate more permissions.
        /// </summary>
        /// <param name="roleName">The role name to check.</param>
        /// <returns>The hierarchy level (0-5).</returns>
        public static int GetRoleHierarchyLevel(string roleName) => roleName switch
        {
            SysAdmin => 5,
            Support => 4,
            Owner => 3,
            Manager => 2,
            Agent => 1,
            _ => 0
        };

        /// <summary>
        /// Determines if one role has higher or equal permissions than another.
        /// </summary>
        /// <param name="userRole">The user's role.</param>
        /// <param name="requiredRole">The required role.</param>
        /// <returns>True if user role has sufficient permissions.</returns>
        public static bool HasSufficientPermissions(string userRole, string requiredRole)
        {
            return GetRoleHierarchyLevel(userRole) >= GetRoleHierarchyLevel(requiredRole);
        }
    }
}
