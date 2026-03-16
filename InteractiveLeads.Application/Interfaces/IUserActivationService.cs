using InteractiveLeads.Application.Feature.Activation;
using InteractiveLeads.Application.Feature.Users;

namespace InteractiveLeads.Application.Interfaces
{
    /// <summary>
    /// Service for user invitation and account activation.
    /// </summary>
    public interface IUserActivationService
    {
        Task<InviteUserResponse> CreateInvitationAsync(InviteUserRequest request, CancellationToken cancellationToken = default);
        Task ActivateAccountAsync(string token, string newPassword, CancellationToken cancellationToken = default);
        /// <summary>
        /// Regenerates an activation token and URL for an existing (still inactive) user.
        /// </summary>
        Task<InviteUserResponse> ResendInvitationAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
