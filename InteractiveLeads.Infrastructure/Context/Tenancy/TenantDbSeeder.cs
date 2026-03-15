using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Infrastructure.Configuration;
using InteractiveLeads.Infrastructure.Context.Application;
using InteractiveLeads.Infrastructure.Context.Tenancy.Interfaces;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InteractiveLeads.Infrastructure.Context.Tenancy
{
    public class TenantDbSeeder : ITenantDbSeeder
    {
        private readonly TenantDbContext _tenantDbContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly SysAdminSeedSettings _sysAdminSeed;

        public TenantDbSeeder(TenantDbContext tenantDbContext, IServiceProvider serviceProvider, IOptions<SysAdminSeedSettings> sysAdminSeed)
        {
            _tenantDbContext = tenantDbContext;
            _serviceProvider = serviceProvider;
            _sysAdminSeed = sysAdminSeed.Value;
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

            if (await _tenantDbContext.TenantInfo.FindAsync([_sysAdminSeed.RootId], cancellationToken) is null)
            {
                if (string.IsNullOrWhiteSpace(_sysAdminSeed.Email))
                    throw new InvalidOperationException("SysAdminSeed:Email is required in appsettings to create the root tenant.");

                var rootTenant = new InteractiveTenantInfo
                {
                    Id = _sysAdminSeed.RootId,
                    Identifier = _sysAdminSeed.RootId,
                    Name = _sysAdminSeed.RootName,
                    Email = _sysAdminSeed.Email.Trim(),
                    FirstName = _sysAdminSeed.FirstName?.Trim() ?? string.Empty,
                    LastName = _sysAdminSeed.LastName?.Trim() ?? string.Empty,
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
