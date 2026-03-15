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
    }
}
