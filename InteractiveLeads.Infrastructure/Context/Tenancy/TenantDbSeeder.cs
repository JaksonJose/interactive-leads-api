using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Infrastructure.Context.Application;
using InteractiveLeads.Infrastructure.Context.Tenancy.Interfaces;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Infrastructure.Context.Tenancy
{
    /// <summary>
    /// Seeds tenant DB and application DB. No root tenant. Roles and SysAdmin are seeded once in global context (TenantInfo with Id = null).
    /// </summary>
    public class TenantDbSeeder : ITenantDbSeeder
    {
        private static readonly InteractiveTenantInfo GlobalContext = new()
        {
            Id = null,
            Identifier = null,
            Name = "Global",
            IsActive = true
        };

        private readonly TenantDbContext _tenantDbContext;
        private readonly IServiceProvider _serviceProvider;

        public TenantDbSeeder(TenantDbContext tenantDbContext, IServiceProvider serviceProvider)
        {
            _tenantDbContext = tenantDbContext;
            _serviceProvider = serviceProvider;
        }

        public async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
        {
            await _tenantDbContext.Database.MigrateAsync(cancellationToken);
            await BillingSeed.SeedAsync(_tenantDbContext, cancellationToken);
            await BillingSeed.EnsureDefaultPlanPricesAsync(_tenantDbContext, cancellationToken);
            await BillingSeed.MigrateExistingTenantsToSubscriptionAsync(_tenantDbContext, cancellationToken);
            await BillingSeed.BackfillSubscriptionPlanPriceAsync(_tenantDbContext, cancellationToken);

            // Single seed run in global context: same roles for all, SysAdmin with TenantId = null
            using var scope = _serviceProvider.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext = new MultiTenantContext<InteractiveTenantInfo>
            {
                TenantInfo = GlobalContext,
            };

            await scope.ServiceProvider.GetRequiredService<ApplicationDbSeeder>().InitializeDatabaseAsync(cancellationToken);
        }
    }
}
