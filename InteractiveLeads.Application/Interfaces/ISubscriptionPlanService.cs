using InteractiveLeads.Application.Models;

namespace InteractiveLeads.Application.Interfaces
{
    /// <summary>
    /// Provides subscription and plan data from the database. Used for access control, limit enforcement, and feature flags.
    /// All limits and features come from the DB; nothing is hardcoded.
    /// </summary>
    public interface ISubscriptionPlanService
    {
        /// <summary>
        /// Gets the tenant's active subscription (Status = Active and EndDate >= today), or null if none.
        /// </summary>
        Task<ActiveSubscriptionInfo?> GetActiveSubscriptionAsync(string tenantId, CancellationToken ct = default);

        /// <summary>
        /// Gets limit key to value for the plan. Empty if plan has no limits.
        /// </summary>
        Task<IReadOnlyDictionary<string, int>> GetPlanLimitsAsync(Guid planId, CancellationToken ct = default);

        /// <summary>
        /// Gets feature keys enabled for the plan. Empty if plan has no features.
        /// </summary>
        Task<IReadOnlySet<string>> GetPlanFeaturesAsync(Guid planId, CancellationToken ct = default);

        /// <summary>
        /// Returns true if adding <paramref name="requestedDelta"/> to <paramref name="currentCount"/> does not exceed the tenant's plan limit for <paramref name="limitKey"/>.
        /// If the plan has no limit for the key, returns true (unlimited).
        /// </summary>
        Task<bool> CheckLimitAsync(string tenantId, string limitKey, int currentCount, int requestedDelta, CancellationToken ct = default);

        /// <summary>
        /// Gets the set of feature keys enabled for the tenant's active plan. Empty if no active subscription or plan has no features.
        /// </summary>
        Task<IReadOnlySet<string>> GetEnabledFeaturesAsync(string tenantId, CancellationToken ct = default);
    }
}
