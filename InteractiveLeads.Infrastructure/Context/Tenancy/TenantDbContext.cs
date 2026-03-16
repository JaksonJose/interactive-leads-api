using Finbuckle.MultiTenant.EntityFrameworkCore.Stores.EFCoreStore;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Context.Tenancy
{
    public class TenantDbContext(DbContextOptions<TenantDbContext> options)
        : EFCoreStoreDbContext<InteractiveTenantInfo>(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<InteractiveTenantInfo>()
                .ToTable("Tenants", "Multitenancy");
        }
    }
}
