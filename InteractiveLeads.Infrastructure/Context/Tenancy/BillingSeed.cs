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
