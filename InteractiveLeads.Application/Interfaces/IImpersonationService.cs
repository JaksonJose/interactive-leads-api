using InteractiveLeads.Application.Feature.Identity.Tokens;
using InteractiveLeads.Application.Responses;

namespace InteractiveLeads.Application.Interfaces
{
    /// <summary>
    /// Service for impersonating another user (SysAdmin/Support only).
    /// </summary>
    public interface IImpersonationService
    {
        /// <summary>
        /// Issues a new JWT (and refresh token) for the target user. Only SysAdmin or Support can call.
        /// </summary>
        /// <param name="targetUserId">The user to impersonate.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Token response for the target user, with impersonated_by claim for audit.</returns>
        Task<SingleResponse<TokenResponse>> ImpersonateAsync(Guid targetUserId, CancellationToken ct = default);
    }
}
