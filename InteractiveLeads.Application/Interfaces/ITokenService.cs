using InteractiveLeads.Application.Feature.Identity.Impersonation;
using InteractiveLeads.Application.Feature.Identity.Tokens;
using InteractiveLeads.Application.Responses;

namespace InteractiveLeads.Application.Interfaces
{
    /// <summary>
    /// Service interface for handling JWT token operations.
    /// </summary>
    /// <remarks>
    /// Provides methods for user authentication, token refresh, and impersonation token generation.
    /// </remarks>
    public interface ITokenService
    {
        /// <summary>
        /// Generates JWT and refresh token for the target user (impersonation). Caller must set tenant context to target user's tenant before calling.
        /// </summary>
        /// <param name="targetUser">Target user data (from lookup).</param>
        /// <param name="impersonatedByUserId">Id of the user performing impersonation (SysAdmin/Support).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Token response with JWT containing impersonated_by claim.</returns>
        Task<SingleResponse<TokenResponse>> GenerateTokenForImpersonationAsync(UserLookupResult targetUser, Guid impersonatedByUserId, CancellationToken ct = default);
        /// <summary>
        /// Authenticates a user and generates JWT tokens.
        /// </summary>
        /// <param name="request">The login credentials containing username and password.</param>
        /// <param name="ct">Cancellation token for the async operation.</param>
        /// <returns>Token response with JWT and refresh token.</returns>
        Task<SingleResponse<TokenResponse>> LoginAsync(TokenRequest request, CancellationToken ct = default);

        /// <summary>
        /// Refreshes an expired JWT access token using a valid refresh token.
        /// </summary>
        /// <param name="request">The refresh token request containing current tokens.</param>
        /// <param name="ct">Cancellation token for the async operation.</param>
        /// <returns>Token response with new JWT and refresh token.</returns>
        Task<SingleResponse<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);

        /// <summary>
        /// Revokes all active refresh tokens for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user whose refresh tokens should be revoked.</param>
        /// <returns>A result response indicating success or failure.</returns>
        Task<ResultResponse> RevokeUserRefreshTokensAsync(Guid userId);

        /// <summary>
        /// Revokes a specific refresh token for a user (useful for logout from current device).
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="refreshToken">The refresh token to revoke.</param>
        /// <returns>A result response indicating success or failure.</returns>
        Task<ResultResponse> RevokeSpecificRefreshTokenAsync(Guid userId, string refreshToken);
    }
}
