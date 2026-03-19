using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
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

        public async Task<InteractiveTenantInfo?> GetAsync(string id)
        {
            if (string.IsNullOrEmpty(id) || id == TenancyConstants.GlobalTenantIdentifier)
                return GlobalTenantInfo;

            return await _inner.GetAsync(id);
        }

        public Task<InteractiveTenantInfo?> GetByIdentifierAsync(string identifier)
        {
            if (string.IsNullOrEmpty(identifier) || identifier == TenancyConstants.GlobalTenantIdentifier)
                return Task.FromResult<InteractiveTenantInfo?>(GlobalTenantInfo);
            return _inner.GetByIdentifierAsync(identifier);
        }

        public Task<bool> AddAsync(InteractiveTenantInfo tenantInfo) => _inner.AddAsync(tenantInfo);

        public Task<bool> UpdateAsync(InteractiveTenantInfo tenantInfo) => _inner.UpdateAsync(tenantInfo);

        public Task<bool> RemoveAsync(string id) => _inner.RemoveAsync(id);
    }
}
