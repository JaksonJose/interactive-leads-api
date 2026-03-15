using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Infrastructure.Constants;
using InteractiveLeads.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;

namespace InteractiveLeads.Infrastructure.Tenancy.Strategies
{
    /// <summary>
    /// Fallback strategy that resolves tenant from the authenticated user's JWT "tenant" claim
    /// when no tenant was resolved from the header (e.g. client did not send X-Tenant).
    /// This ensures the ApplicationDbContext has a valid tenant context for authorization lookups
    /// (e.g. finding the current user in their home tenant's database).
    /// Header strategy takes precedence when present.
    /// </summary>
    public class JwtTenantFallbackStrategy : IMultiTenantStrategy
    {
        /// <inheritdoc />
        public Task<string?> GetIdentifierAsync(object context)
        {
            if (context is not HttpContext httpContext)
                return Task.FromResult<string?>(null);

            if (!(httpContext.User?.Identity?.IsAuthenticated ?? false))
                return Task.FromResult<string?>(null);

            var tenantId = httpContext.User.GetTenant();
            return Task.FromResult(string.IsNullOrEmpty(tenantId) ? null : tenantId);
        }
    }
}
