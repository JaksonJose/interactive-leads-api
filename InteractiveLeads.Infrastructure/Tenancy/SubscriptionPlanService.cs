using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Models;
using InteractiveLeads.Infrastructure.Context.Tenancy;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Tenancy
{
    public sealed class SubscriptionPlanService : ISubscriptionPlanService
    {
        private readonly TenantDbContext _tenantDbContext;

        public SubscriptionPlanService(TenantDbContext tenantDbContext)
        {
            _tenantDbContext = tenantDbContext;
        }

        public async Task<ActiveSubscriptionInfo?> GetActiveSubscriptionAsync(string tenantId, CancellationToken ct = default)
        {
            var today = DateTime.UtcNow.Date;
            var sub = await _tenantDbContext.Subscriptions
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active && s.EndDate >= today)
                .OrderByDescending(s => s.EndDate)
                .Select(s => new { s.Id, s.PlanId, s.EndDate })
                .FirstOrDefaultAsync(ct);

            if (sub == null)
                return null;

            return new ActiveSubscriptionInfo
            {
                SubscriptionId = sub.Id,
                PlanId = sub.PlanId,
                EndDate = sub.EndDate
            };
        }

        public async Task<IReadOnlyDictionary<string, int>> GetPlanLimitsAsync(Guid planId, CancellationToken ct = default)
        {
            var list = await _tenantDbContext.PlanLimits
                .AsNoTracking()
                .Where(l => l.PlanId == planId)
                .Select(l => new { l.LimitKey, l.LimitValue })
                .ToListAsync(ct);
            return list.ToDictionary(x => x.LimitKey, x => x.LimitValue);
        }

        public async Task<IReadOnlySet<string>> GetPlanFeaturesAsync(Guid planId, CancellationToken ct = default)
        {
            var list = await _tenantDbContext.PlanFeatures
                .AsNoTracking()
                .Where(f => f.PlanId == planId)
                .Select(f => f.FeatureKey)
                .ToListAsync(ct);
            return list.ToHashSet();
        }

        public async Task<bool> CheckLimitAsync(string tenantId, string limitKey, int currentCount, int requestedDelta, CancellationToken ct = default)
        {
            var sub = await GetActiveSubscriptionAsync(tenantId, ct);
            if (sub == null)
                return false;

            var limits = await GetPlanLimitsAsync(sub.PlanId, ct);
            if (!limits.TryGetValue(limitKey, out var limitValue))
                return true; // no limit defined = unlimited

            return currentCount + requestedDelta <= limitValue;
        }

        public async Task<IReadOnlySet<string>> GetEnabledFeaturesAsync(string tenantId, CancellationToken ct = default)
        {
            var sub = await GetActiveSubscriptionAsync(tenantId, ct);
            if (sub == null)
                return new HashSet<string>(0);

            return await GetPlanFeaturesAsync(sub.PlanId, ct);
        }
    }
}
