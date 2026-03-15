using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Stores.EFCoreStore;
using InteractiveLeads.Infrastructure.Constants;
using InteractiveLeads.Infrastructure.Context.Tenancy;
using InteractiveLeads.Infrastructure.Tenancy.Models;

namespace InteractiveLeads.Infrastructure.Tenancy
{
    /// <summary>
    /// Wraps the EFCore tenant store and returns a synthetic TenantInfo with Id = null for global users (SysAdmin, Support).
    /// When strategy returns <see cref="TenancyConstants.GlobalTenantIdentifier"/>, the filter becomes TenantId IS NULL.
    /// </summary>
    public class GlobalTenantStoreWrapper : IMultiTenantStore<InteractiveTenantInfo>
    {
        private static readonly InteractiveTenantInfo GlobalTenantInfo = new()
        {
            Id = null,
            Identifier = null,
            Name = "Global",
            IsActive = true
        };

        private readonly IMultiTenantStore<InteractiveTenantInfo> _inner;

        public GlobalTenantStoreWrapper(TenantDbContext context)
        {
            _inner = new EFCoreStore<TenantDbContext, InteractiveTenantInfo>(context);
        }

        public Task<IEnumerable<InteractiveTenantInfo>> GetAllAsync() => _inner.GetAllAsync();

        public Task<IEnumerable<InteractiveTenantInfo>> GetAllAsync(int skip, int take)
            => _inner.GetAllAsync(skip, take);

        public async Task<InteractiveTenantInfo?> TryGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id) || id == TenancyConstants.GlobalTenantIdentifier)
                return GlobalTenantInfo;

            return await _inner.TryGetAsync(id);
        }

        public Task<InteractiveTenantInfo?> TryGetByIdentifierAsync(string identifier)
        {
            if (string.IsNullOrEmpty(identifier) || identifier == TenancyConstants.GlobalTenantIdentifier)
                return Task.FromResult<InteractiveTenantInfo?>(GlobalTenantInfo);
            return _inner.TryGetByIdentifierAsync(identifier);
        }

        public Task<bool> TryAddAsync(InteractiveTenantInfo tenantInfo) => _inner.TryAddAsync(tenantInfo);

        public Task<bool> TryUpdateAsync(InteractiveTenantInfo tenantInfo) => _inner.TryUpdateAsync(tenantInfo);

        public Task<bool> TryRemoveAsync(string id) => _inner.TryRemoveAsync(id);
    }
}
