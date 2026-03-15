using InteractiveLeads.Application.Feature.Identity.Impersonation;

namespace InteractiveLeads.Application.Interfaces
{
    /// <summary>
    /// Looks up user data by id without tenant filter (e.g. for impersonation).
    /// </summary>
    public interface IUserLookupService
    {
        /// <summary>
        /// Gets user data by id, bypassing tenant scope.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The user lookup result, or null if not found.</returns>
        Task<UserLookupResult?> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
    }
}
