using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;

namespace InteractiveLeads.Infrastructure.Tenancy.Strategies
{
    /// <summary>
    /// Fallback strategy when no tenant was resolved from the header (e.g. X-Tenant).
    /// With pure RBAC we do not store tenant in the JWT; clients must send X-Tenant for tenant context.
    /// This strategy returns null so that tenant context remains unset when the header is missing.
    /// </summary>
    public class JwtTenantFallbackStrategy : IMultiTenantStrategy
    {
        /// <inheritdoc />
        public Task<string?> GetIdentifierAsync(object context)
        {
            return Task.FromResult<string?>(null);
        }
    }
}
