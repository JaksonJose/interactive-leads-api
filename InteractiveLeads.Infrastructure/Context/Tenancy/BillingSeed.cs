using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Context.Tenancy
{
    /// <summary>
    /// Seeds default plan and limits/features in the tenant store. Idempotent: runs only when no plans exist.
    /// </summary>
    public static class BillingSeed
    {
        public const string DefaultPlanIdentifier = "default";
        public const string DefaultPlanName = "Default";

        /// <summary>Limit keys used by the system. Values come from DB only.</summary>
        public static class LimitKeys
        {
            public const string Users = "users";
            public const string WhatsAppNumbers = "whatsapp_numbers";
            public const string Leads = "leads";
            public const string StorageMb = "storage_mb";
        }

        public static async Task SeedAsync(TenantDbContext context, CancellationToken ct = default)
        {
            if (await context.Plans.AnyAsync(ct))
                return;

            var planId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            context.Plans.Add(new Plan
            {
                Id = planId,
                Name = DefaultPlanName,
                Identifier = DefaultPlanIdentifier,
                IsActive = true,
                CreatedAt = now
            });

            context.PlanLimits.AddRange(
                new PlanLimit { Id = Guid.NewGuid(), PlanId = planId, LimitKey = LimitKeys.Users, LimitValue = 10 },
                new PlanLimit { Id = Guid.NewGuid(), PlanId = planId, LimitKey = LimitKeys.WhatsAppNumbers, LimitValue = 0 },
                new PlanLimit { Id = Guid.NewGuid(), PlanId = planId, LimitKey = LimitKeys.Leads, LimitValue = 1000 },
                new PlanLimit { Id = Guid.NewGuid(), PlanId = planId, LimitKey = LimitKeys.StorageMb, LimitValue = 100 }
            );

            context.PlanPrices.AddRange(
                new PlanPrice { Id = Guid.NewGuid(), PlanId = planId, Price = 400m, Currency = "BRL", BillingInterval = BillingInterval.Month, IntervalCount = 1, IsActive = true, CreatedAt = now },
                new PlanPrice { Id = Guid.NewGuid(), PlanId = planId, Price = 1080m, Currency = "BRL", BillingInterval = BillingInterval.Month, IntervalCount = 3, IsActive = true, CreatedAt = now },
                new PlanPrice { Id = Guid.NewGuid(), PlanId = planId, Price = 3840m, Currency = "BRL", BillingInterval = BillingInterval.Year, IntervalCount = 1, IsActive = true, CreatedAt = now }
            );

            await context.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Ensures the default plan has at least one plan price (for existing DBs that had plans before PlanPrice existed).
        /// Idempotent: only adds prices when the default plan has none.
        /// </summary>
        public static async Task EnsureDefaultPlanPricesAsync(TenantDbContext context, CancellationToken ct = default)
        {
            var defaultPlan = await context.Plans
                .FirstOrDefaultAsync(p => p.Identifier == DefaultPlanIdentifier, ct);
            if (defaultPlan == null)
                return;
            var hasPrices = await context.PlanPrices.AnyAsync(pp => pp.PlanId == defaultPlan.Id, ct);
            if (hasPrices)
                return;

            var now = DateTime.UtcNow;
            context.PlanPrices.AddRange(
                new PlanPrice { Id = Guid.NewGuid(), PlanId = defaultPlan.Id, Price = 400m, Currency = "BRL", BillingInterval = BillingInterval.Month, IntervalCount = 1, IsActive = true, CreatedAt = now },
                new PlanPrice { Id = Guid.NewGuid(), PlanId = defaultPlan.Id, Price = 1080m, Currency = "BRL", BillingInterval = BillingInterval.Month, IntervalCount = 3, IsActive = true, CreatedAt = now },
                new PlanPrice { Id = Guid.NewGuid(), PlanId = defaultPlan.Id, Price = 3840m, Currency = "BRL", BillingInterval = BillingInterval.Year, IntervalCount = 1, IsActive = true, CreatedAt = now }
            );
            await context.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Gets the first active plan price for the default plan. Returns null if none.
        /// </summary>
        public static async Task<PlanPrice?> GetDefaultPlanPriceAsync(TenantDbContext context, CancellationToken ct = default)
        {
            var defaultPlan = await context.Plans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Identifier == DefaultPlanIdentifier, ct);
            if (defaultPlan == null)
                return null;
            return await GetFirstActivePlanPriceForPlanAsync(context, defaultPlan.Id, ct);
        }

        /// <summary>
        /// Gets the first active plan price for the given plan. Returns null if none.
        /// </summary>
        public static async Task<PlanPrice?> GetFirstActivePlanPriceForPlanAsync(TenantDbContext context, Guid planId, CancellationToken ct = default)
        {
            return await context.PlanPrices
                .AsNoTracking()
                .Where(pp => pp.PlanId == planId && pp.IsActive)
                .OrderBy(pp => pp.BillingInterval).ThenBy(pp => pp.IntervalCount)
                .FirstOrDefaultAsync(ct);
        }

        /// <summary>
        /// Backfill PlanPriceId on existing subscriptions that have default plan but null PlanPriceId.
        /// </summary>
        public static async Task BackfillSubscriptionPlanPriceAsync(TenantDbContext context, CancellationToken ct = default)
        {
            var defaultPlan = await context.Plans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Identifier == DefaultPlanIdentifier, ct);
            if (defaultPlan == null)
                return;

            var defaultPrice = await context.PlanPrices
                .AsNoTracking()
                .Where(pp => pp.PlanId == defaultPlan.Id && pp.IsActive)
                .OrderBy(pp => pp.BillingInterval).ThenBy(pp => pp.IntervalCount)
                .FirstOrDefaultAsync(ct);
            if (defaultPrice == null)
                return;

            var subsWithoutPrice = await context.Subscriptions
                .Where(s => s.PlanId == defaultPlan.Id && s.PlanPriceId == null)
                .ToListAsync(ct);
            foreach (var sub in subsWithoutPrice)
                sub.PlanPriceId = defaultPrice.Id;
            if (subsWithoutPrice.Count > 0)
                await context.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Creates an active subscription for each existing tenant that does not have one. Uses default plan and tenant ExpirationDate.
        /// </summary>
        public static async Task MigrateExistingTenantsToSubscriptionAsync(TenantDbContext context, CancellationToken ct = default)
        {
            var defaultPlan = await context.Plans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Identifier == DefaultPlanIdentifier, ct);
            if (defaultPlan == null)
                return;

            var defaultPrice = await GetFirstActivePlanPriceForPlanAsync(context, defaultPlan.Id, ct);
            var today = DateTime.UtcNow.Date;
            var tenants = await context.TenantInfo
                .AsNoTracking()
                .Select(t => new { t.Id, t.ExpirationDate })
                .ToListAsync(ct);

            foreach (var t in tenants)
            {
                if (string.IsNullOrEmpty(t.Id))
                    continue;
                var hasActive = await context.Subscriptions
                    .AnyAsync(s => s.TenantId == t.Id && s.Status == SubscriptionStatus.Active && s.EndDate >= today, ct);
                if (hasActive)
                    continue;

                var now = DateTime.UtcNow;
                context.Subscriptions.Add(new Subscription
                {
                    Id = Guid.NewGuid(),
                    TenantId = t.Id,
                    PlanId = defaultPlan.Id,
                    PlanPriceId = defaultPrice?.Id,
                    Status = SubscriptionStatus.Active,
                    StartDate = now,
                    EndDate = t.ExpirationDate,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            await context.SaveChangesAsync(ct);
        }
    }
}
