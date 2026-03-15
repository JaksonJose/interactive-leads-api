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
        ApplicationRoleClaim,
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
        }
    }
}
