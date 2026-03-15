using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Infrastructure.Identity.Models;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Context.Application
{
    public class ApplicationDbContext : BaseDbContext
    {
        public ApplicationDbContext(
            IMultiTenantContextAccessor<InteractiveTenantInfo> tenantContextAccessor,
            DbContextOptions<ApplicationDbContext> options)
            : base(tenantContextAccessor, options)
        {
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
    }
}
