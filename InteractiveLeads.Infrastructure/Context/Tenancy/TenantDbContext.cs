using Finbuckle.MultiTenant.EntityFrameworkCore.Stores.EFCoreStore;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Context.Tenancy
{
    public class TenantDbContext(DbContextOptions<TenantDbContext> options)
        : EFCoreStoreDbContext<InteractiveTenantInfo>(options)
    {
        public DbSet<Plan> Plans => Set<Plan>();
        public DbSet<PlanLimit> PlanLimits => Set<PlanLimit>();
        public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();
        public DbSet<PlanPrice> PlanPrices => Set<PlanPrice>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<InteractiveTenantInfo>()
                .ToTable("Tenants", "Multitenancy");

            modelBuilder.Entity<Plan>(b =>
            {
                b.ToTable("Plans", "Multitenancy");
                b.HasKey(e => e.Id);
                b.HasIndex(e => e.Identifier).IsUnique();
                b.Property(e => e.Name).HasMaxLength(256).IsRequired();
                b.Property(e => e.Identifier).HasMaxLength(64).IsRequired();
            });

            modelBuilder.Entity<PlanLimit>(b =>
            {
                b.ToTable("PlanLimits", "Multitenancy");
                b.HasKey(e => e.Id);
                b.HasIndex(e => new { e.PlanId, e.LimitKey }).IsUnique();
                b.Property(e => e.LimitKey).HasMaxLength(64).IsRequired();
                b.HasOne(e => e.Plan).WithMany().HasForeignKey(e => e.PlanId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PlanFeature>(b =>
            {
                b.ToTable("PlanFeatures", "Multitenancy");
                b.HasKey(e => e.Id);
                b.HasIndex(e => new { e.PlanId, e.FeatureKey }).IsUnique();
                b.Property(e => e.FeatureKey).HasMaxLength(64).IsRequired();
                b.HasOne(e => e.Plan).WithMany().HasForeignKey(e => e.PlanId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PlanPrice>(b =>
            {
                b.ToTable("PlanPrices", "Multitenancy");
                b.HasKey(e => e.Id);
                b.Property(e => e.Currency).HasMaxLength(10).IsRequired();
                b.HasIndex(e => new { e.PlanId, e.BillingInterval, e.IntervalCount }).IsUnique();
                b.HasOne(e => e.Plan).WithMany().HasForeignKey(e => e.PlanId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Subscription>(b =>
            {
                b.ToTable("Subscriptions", "Multitenancy");
                b.HasKey(e => e.Id);
                b.HasOne(e => e.Plan).WithMany().HasForeignKey(e => e.PlanId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(e => e.PlanPrice).WithMany().HasForeignKey(e => e.PlanPriceId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                // At most one active subscription per tenant (SubscriptionStatus.Active = 0)
                b.HasIndex(e => e.TenantId)
                    .HasFilter("\"Status\" = 0")
                    .IsUnique();
            });
        }
    }
}
