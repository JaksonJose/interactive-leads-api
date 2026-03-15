using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Infrastructure.Constants;
using InteractiveLeads.Infrastructure.Context.Application;
using InteractiveLeads.Infrastructure.Context.Tenancy.Interfaces;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Infrastructure.Context.Tenancy
{
    public class TenantDbSeeder : ITenantDbSeeder
    {
        private readonly TenantDbContext _tenantDbContext;
        private readonly IServiceProvider _serviceProvider;

        public TenantDbSeeder(TenantDbContext tenantDbContext, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _tenantDbContext = tenantDbContext;
        }

        public async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
        {
            // seed tenant data
            await InitializeDatabaseWithTenantAsync(cancellationToken);

            foreach (var tenant in await _tenantDbContext.TenantInfo.ToListAsync(cancellationToken))
            {
                // Application Db seeder
                await InitializeApplicationDbForTenantAsync(tenant, cancellationToken);
            }
        }

        private async Task InitializeDatabaseWithTenantAsync(CancellationToken cancellationToken)
        {
            await _tenantDbContext.Database.MigrateAsync(cancellationToken);

            if (await _tenantDbContext.TenantInfo.FindAsync([TenancyConstants.Root.Id], cancellationToken) is null) 
            {
                // Create tenant
                var rootTenant = new InteractiveTenantInfo
                {
                    Id = TenancyConstants.Root.Id,
                    Identifier = TenancyConstants.Root.Id,
                    Name = TenancyConstants.Root.Name,
                    Email = TenancyConstants.Root.Email,
                    FirstName = TenancyConstants.FirstName,
                    LastName = TenancyConstants.LastName,
                    IsActive = true,
                    ExpirationDate = DateTime.UtcNow.AddYears(1)
                };

                await _tenantDbContext.TenantInfo.AddAsync(rootTenant, cancellationToken);
                await _tenantDbContext.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task InitializeApplicationDbForTenantAsync(InteractiveTenantInfo currentTenant, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateAsyncScope();

            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext = new MultiTenantContext<InteractiveTenantInfo>()
            {
                TenantInfo = currentTenant,
            };

            await scope.ServiceProvider.GetRequiredService<ApplicationDbSeeder>().InitializeDatabaseAsync(ct);
        }
    }
}
