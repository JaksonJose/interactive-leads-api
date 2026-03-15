using System.Security.Claims;
using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Infrastructure.Constants;
using Microsoft.AspNetCore.Http;

namespace InteractiveLeads.Infrastructure.Tenancy.Strategies
{
    /// <summary>
    /// Resolves tenant from JWT claim "tenant_id". When claim is missing or empty (global users), returns GlobalTenantIdentifier so store returns TenantInfo with Id = null.
    /// </summary>
    public class JwtTenantFallbackStrategy : IMultiTenantStrategy
    {
        public const string TenantIdClaimType = "tenant_id";

        /// <summary>
        /// Claim type for audit: ID of the user who started impersonation (SysAdmin/Support).
        /// </summary>
        public const string ImpersonatedByClaimType = "impersonated_by";

        public Task<string?> GetIdentifierAsync(object context)
        {
            if (context is not HttpContext httpContext)
                return Task.FromResult<string?>(null);

            var tenantId = httpContext.User?.FindFirst(TenantIdClaimType)?.Value;
            // Empty or null claim => global context (TenantInfo with Id = null)
            if (string.IsNullOrEmpty(tenantId))
                return Task.FromResult<string?>(TenancyConstants.GlobalTenantIdentifier);

            return Task.FromResult<string?>(tenantId);
        }
    }
}
