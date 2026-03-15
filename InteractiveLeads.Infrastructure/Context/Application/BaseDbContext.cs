using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using InteractiveLeads.Infrastructure.Identity.Models;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace InteractiveLeads.Infrastructure.Context.Application
{
    public abstract class BaseDbContext : MultiTenantIdentityDbContext<ApplicationUser,
        ApplicationRole,
        Guid,
        IdentityUserClaim<Guid>,
        IdentityUserRole<Guid>,
        IdentityUserLogin<Guid>,
        IdentityRoleClaim<Guid>,
        IdentityUserToken<Guid>>
    {
        private new InteractiveTenantInfo TenantInfo { get; set; }

        protected BaseDbContext(IMultiTenantContextAccessor<InteractiveTenantInfo> tenantContextAccessor, DbContextOptions options) 
            : base(tenantContextAccessor, options)
        {
            TenantInfo = tenantContextAccessor.MultiTenantContext.TenantInfo ?? default!;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            // When in global context (TenantInfo.Id is null), overwrite so entities get TenantId = null (no exception).
            if (TenantInfo?.Id == null)
                TenantNotSetMode = TenantNotSetMode.Overwrite;

            if (!string.IsNullOrWhiteSpace(TenantInfo?.ConnectionString)) 
            {
                optionsBuilder.UseNpgsql(TenantInfo.ConnectionString, options =>
                {
                    options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                    options.EnableRetryOnFailure();
                });
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(GetType().Assembly);

            // Pure RBAC: do not map role claims to any table (no RoleClaims table).
            builder.Ignore<IdentityRoleClaim<Guid>>();
        }
    }
}
