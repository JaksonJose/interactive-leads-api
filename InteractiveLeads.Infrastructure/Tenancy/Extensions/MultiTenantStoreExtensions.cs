using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Infrastructure.Tenancy.Models;

namespace InteractiveLeads.Infrastructure.Tenancy.Extensions
{
    /// <summary>
    /// Extension methods for IMultiTenantStore to provide additional query capabilities.
    /// </summary>
    public static class MultiTenantStoreExtensions
    {
        /// <summary>
        /// Finds a tenant by email address.
        /// </summary>
        /// <param name="tenantStore">The tenant store instance.</param>
        /// <param name="email">The email address to search for.</param>
        /// <returns>The tenant information if found, otherwise null.</returns>
        public static async Task<InteractiveTenantInfo?> FindByEmailAsync(this IMultiTenantStore<InteractiveTenantInfo> tenantStore, string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var allTenants = await tenantStore.GetAllAsync();
            return allTenants.FirstOrDefault(t => 
                t.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        // Backward-compatible wrappers for Finbuckle v10 API.
        public static Task<InteractiveTenantInfo?> TryGetAsync(this IMultiTenantStore<InteractiveTenantInfo> tenantStore, string id)
            => tenantStore.GetAsync(id);

        public static Task<InteractiveTenantInfo?> TryGetByIdentifierAsync(this IMultiTenantStore<InteractiveTenantInfo> tenantStore, string identifier)
            => tenantStore.GetByIdentifierAsync(identifier);

        public static Task<bool> TryAddAsync(this IMultiTenantStore<InteractiveTenantInfo> tenantStore, InteractiveTenantInfo tenantInfo)
            => tenantStore.AddAsync(tenantInfo);

        public static Task<bool> TryUpdateAsync(this IMultiTenantStore<InteractiveTenantInfo> tenantStore, InteractiveTenantInfo tenantInfo)
            => tenantStore.UpdateAsync(tenantInfo);

        public static Task<bool> TryRemoveAsync(this IMultiTenantStore<InteractiveTenantInfo> tenantStore, string id)
            => tenantStore.RemoveAsync(id);
    }
}
