using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Infrastructure.Constants;
using InteractiveLeads.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Tenancy
{
    /// <summary>
    /// Service for handling cross-tenant authorization operations.
    /// </summary>
    /// <remarks>
    /// Provides functionality to validate cross-tenant access permissions
    /// and determine what tenants a user can access based on their roles.
    /// </remarks>
    public class CrossTenantAuthorizationService : ICrossTenantAuthorizationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ITenantService _tenantService;

        /// <summary>
        /// Initializes a new instance of the CrossTenantAuthorizationService class.
        /// </summary>
        /// <param name="userManager">The user manager for user operations.</param>
        /// <param name="roleManager">The role manager for role operations.</param>
        /// <param name="tenantService">The tenant service for tenant operations.</param>
        public CrossTenantAuthorizationService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ITenantService tenantService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tenantService = tenantService;
        }

        /// <summary>
        /// Determines whether a user can access a specific tenant.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="tenantId">The ID of the tenant to check access for.</param>
        /// <returns>True if the user can access the tenant, otherwise false.</returns>
        public async Task<bool> CanAccessTenantAsync(Guid userId, string tenantId)
        {
            // Handle special case for "all tenants" marker
            if (tenantId == "*")
                return false; // No user can access the "*" marker itself

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            var userRoles = await _userManager.GetRolesAsync(user);
            
            // Cross-tenant roles can access any tenant
            if (RoleConstants.CrossTenantRoles.Any(role => userRoles.Contains(role)))
                return true;
            
            // Tenant-specific roles can only access their own tenant
            if (RoleConstants.TenantRoles.Any(role => userRoles.Contains(role)))
                return user.TenantId == tenantId;           
            
            // Default: users can only access their own tenant
            return user.TenantId == tenantId;
        }

        /// <summary>
        /// Determines whether a user can perform a specific action in a tenant.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <param name="permission">The permission to check.</param>
        /// <returns>True if the user can perform the action, otherwise false.</returns>
        public async Task<bool> CanPerformActionInTenantAsync(Guid userId, string tenantId, string permission)
        {
            if (!await CanAccessTenantAsync(userId, tenantId))
                return false;

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            var userRoles = await _userManager.GetRolesAsync(user);
            
            // SysAdmin can perform any action
            if (userRoles.Contains(RoleConstants.SysAdmin))
                return true;
            
            // Support can read, create and update (not delete)
            if (userRoles.Contains(RoleConstants.Support))
            {
                return permission.Contains("Read") || permission.Contains("Update") || permission.Contains("Create");
            }
            
            // Tenant-specific roles can perform actions within their tenant (authorization by roles only; parametrização may refine later)
            if (RoleConstants.TenantRoles.Any(role => userRoles.Contains(role)))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if a user has access to all tenants (cross-tenant access).
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if the user has cross-tenant access, otherwise false.</returns>
        public async Task<bool> HasAllTenantsAccessAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            var userRoles = await _userManager.GetRolesAsync(user);
            return RoleConstants.CrossTenantRoles.Any(role => userRoles.Contains(role));
        }

        /// <summary>
        /// Gets a paginated list of tenants that a user can access.
        /// This method is more efficient for large numbers of tenants.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="pageNumber">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paginated list of accessible tenant IDs.</returns>
        public async Task<(string[] TenantIds, bool HasMore)> GetAccessibleTenantsPaginatedAsync(
            Guid userId, int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return (Array.Empty<string>(), false);

            var userRoles = await _userManager.GetRolesAsync(user);
            
            // Cross-tenant roles can access all tenants
            if (RoleConstants.CrossTenantRoles.Any(role => userRoles.Contains(role)))
            {
                // For cross-tenant users, we would need to implement pagination
                // from the tenant service. For now, return the special marker.
                return (new[] { "*" }, false);
            }            
            
            // Default: users can only access their own tenant
            return (new[] { user.TenantId }, false);
        }

        /// <summary>
        /// Determines whether a user is a system administrator.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if the user is a system administrator, otherwise false.</returns>
        public async Task<bool> IsSystemAdminAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            var userRoles = await _userManager.GetRolesAsync(user);
            return userRoles.Contains(RoleConstants.SysAdmin);
        }

        /// <summary>
        /// Determines whether a user is a support user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if the user is a support user, otherwise false.</returns>
        public async Task<bool> IsSupportUserAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            var userRoles = await _userManager.GetRolesAsync(user);
            return userRoles.Contains(RoleConstants.Support);
        }

        /// <summary>
        /// Determines whether a user is a tenant owner.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if the user is a tenant owner, otherwise false.</returns>
        public async Task<bool> IsTenantOwnerAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            var userRoles = await _userManager.GetRolesAsync(user);
            return userRoles.Contains(RoleConstants.Owner);
        }

        /// <summary>
        /// Determines whether a user is a tenant manager.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if the user is a tenant manager, otherwise false.</returns>
        public async Task<bool> IsTenantManagerAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            var userRoles = await _userManager.GetRolesAsync(user);
            return userRoles.Contains(RoleConstants.Manager);
        }

        /// <summary>
        /// Determines whether a user is a tenant agent.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if the user is a tenant agent, otherwise false.</returns>
        public async Task<bool> IsTenantAgentAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            var userRoles = await _userManager.GetRolesAsync(user);
            return userRoles.Contains(RoleConstants.Agent);
        }

        /// <summary>
        /// Determines whether a user has any cross-tenant permissions.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if the user has cross-tenant permissions, otherwise false.</returns>
        public async Task<bool> HasCrossTenantPermissionsAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            var userRoles = await _userManager.GetRolesAsync(user);
            return RoleConstants.CrossTenantRoles.Any(role => userRoles.Contains(role));
        }

        /// <summary>
        /// Gets the highest role level for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The highest role level (0-5).</returns>
        public async Task<int> GetUserRoleLevelAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return 0;

            var userRoles = await _userManager.GetRolesAsync(user);
            return userRoles.Max(role => RoleConstants.GetRoleHierarchyLevel(role));
        }

        /// <summary>
        /// Determines whether a user has sufficient permissions for a required role level.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="requiredRoleLevel">The required role level.</param>
        /// <returns>True if the user has sufficient permissions, otherwise false.</returns>
        public async Task<bool> HasSufficientRoleLevelAsync(Guid userId, int requiredRoleLevel)
        {
            var userRoleLevel = await GetUserRoleLevelAsync(userId);
            return userRoleLevel >= requiredRoleLevel;
        }
    }
}
