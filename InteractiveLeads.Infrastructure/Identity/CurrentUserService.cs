using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Infrastructure.Identity;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.AspNetCore.Http;

namespace InteractiveLeads.Infrastructure.Identity
{
    /// <summary>
    /// Service implementation for accessing current user information.
    /// </summary>
    /// <remarks>
    /// Provides methods to access the currently authenticated user's identity from the current HTTP context.
    /// Tenant is resolved from the request's multi-tenant context (e.g. X-Tenant header), not from claims.
    /// </remarks>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMultiTenantContextAccessor<InteractiveTenantInfo> _tenantContextAccessor;

        /// <summary>
        /// Initializes a new instance of the CurrentUserService class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor for accessing the current request context.</param>
        /// <param name="tenantContextAccessor">The multi-tenant context accessor for the current request's tenant.</param>
        public CurrentUserService(IHttpContextAccessor httpContextAccessor, IMultiTenantContextAccessor<InteractiveTenantInfo> tenantContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _tenantContextAccessor = tenantContextAccessor;
        }

        /// <summary>
        /// Gets the name of the current user.
        /// </summary>
        public string Name => _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? string.Empty;

        /// <summary>
        /// Gets the unique identifier of the current user.
        /// </summary>
        /// <returns>User identifier as string.</returns>
        public string GetUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.GetUserId() ?? string.Empty;
        }

        /// <summary>
        /// Gets the email address of the current user.
        /// </summary>
        /// <returns>User email address.</returns>
        public string GetUserEmail()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.GetEmail() ?? string.Empty;
        }

        /// <summary>
        /// Gets the tenant identifier for the current request (from multi-tenant context, e.g. X-Tenant header).
        /// </summary>
        /// <returns>Tenant identifier, or empty if no tenant context.</returns>
        public string GetUserTenant()
        {
            return _tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id ?? string.Empty;
        }

        /// <summary>
        /// Checks if the current user is authenticated.
        /// </summary>
        /// <returns>True if user is authenticated, false otherwise.</returns>
        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }

        /// <summary>
        /// Checks if the current user is in the specified role.
        /// </summary>
        /// <param name="roleName">Name of the role to check.</param>
        /// <returns>True if user is in the role, false otherwise.</returns>
        public bool IsInRole(string roleName)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.IsInRole(roleName) ?? false;
        }

    }
}
