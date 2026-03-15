namespace InteractiveLeads.Application.Interfaces
{
    /// <summary>
    /// Service interface for cross-tenant authorization operations.
    /// </summary>
    /// <remarks>
    /// Provides functionality to validate cross-tenant access permissions
    /// and determine what tenants a user can access based on their roles.
    /// </remarks>
    public interface ICrossTenantAuthorizationService
    {
        /// <summary>
        /// Determines whether a user can access a specific tenant.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="tenantId">The ID of the tenant to check access for.</param>
        /// <returns>True if the user can access the tenant, otherwise false.</returns>
        /// <remarks>
        /// This method checks if the user has cross-tenant permissions and
        /// whether they are allowed to access the specific tenant.
        /// </remarks>
        Task<bool> CanAccessTenantAsync(Guid userId, string tenantId);

        /// <summary>
        /// Determines whether a user can perform a specific action in a tenant.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <param name="permission">The permission to check.</param>
        /// <returns>True if the user can perform the action, otherwise false.</returns>
        /// <remarks>
        /// This method validates both tenant access and specific permission
        /// for the requested action.
        /// </remarks>
        Task<bool> CanPerformActionInTenantAsync(Guid userId, string tenantId, string permission);

        /// <summary>
        /// Determines whether a user is a system administrator.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if the user is a system administrator, otherwise false.</returns>
        /// <remarks>
        /// System administrators have full access to all tenants and operations.
        /// </remarks>
        Task<bool> IsSystemAdminAsync(Guid userId);

        /// <summary>
        /// Determines whether a user is a support user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if the user is a support user, otherwise false.</returns>
        /// <remarks>
        /// Support users have limited cross-tenant access for customer support operations.
        /// </remarks>
        Task<bool> IsSupportUserAsync(Guid userId);

        /// <summary>
        /// Determines whether a user has any cross-tenant permissions.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if the user has cross-tenant permissions, otherwise false.</returns>
        /// <remarks>
        /// This includes SysAdmin and Support roles.
        /// </remarks>
        Task<bool> HasCrossTenantPermissionsAsync(Guid userId);

        /// <summary>
        /// Checks if a user has access to all tenants (cross-tenant access).
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if the user has cross-tenant access, otherwise false.</returns>
        /// <remarks>
        /// This method is more efficient than GetAccessibleTenantsAsync for checking
        /// if a user has cross-tenant permissions without loading tenant lists.
        /// </remarks>
        Task<bool> HasAllTenantsAccessAsync(Guid userId);

        /// <summary>
        /// Gets a paginated list of tenants that a user can access.
        /// This method is more efficient for large numbers of tenants.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="pageNumber">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paginated list of accessible tenant IDs.</returns>
        /// <remarks>
        /// For cross-tenant users, returns "*" indicating access to all tenants.
        /// For tenant-specific users, returns their specific tenant.
        /// </remarks>
        Task<(string[] TenantIds, bool HasMore)> GetAccessibleTenantsPaginatedAsync(
            Guid userId, int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines whether a user is a tenant owner.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if the user is a tenant owner, otherwise false.</returns>
        Task<bool> IsTenantOwnerAsync(Guid userId);

        /// <summary>
        /// Determines whether a user is a tenant manager.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if the user is a tenant manager, otherwise false.</returns>
        Task<bool> IsTenantManagerAsync(Guid userId);

        /// <summary>
        /// Determines whether a user is a tenant agent.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if the user is a tenant agent, otherwise false.</returns>
        Task<bool> IsTenantAgentAsync(Guid userId);

        /// <summary>
        /// Gets the highest role level for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The highest role level (0-5).</returns>
        Task<int> GetUserRoleLevelAsync(Guid userId);

        /// <summary>
        /// Determines whether a user has sufficient permissions for a required role level.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="requiredRoleLevel">The required role level.</param>
        /// <returns>True if the user has sufficient permissions, otherwise false.</returns>
        Task<bool> HasSufficientRoleLevelAsync(Guid userId, int requiredRoleLevel);
    }
}
