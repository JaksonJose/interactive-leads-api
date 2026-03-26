using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Activation;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Application.Feature.CrossTenant.Commands
{
    /// <summary>
    /// Command to resend an activation invitation for a user in a tenant.
    /// </summary>
    public sealed class ResendUserInvitationInTenantCommand : IApplicationRequest<IResponse>, IValidate
    {
        public string TenantId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
    }

    public sealed class ResendUserInvitationInTenantCommandHandler : IApplicationRequestHandler<ResendUserInvitationInTenantCommand, IResponse>
    {
        private readonly ICrossTenantService _crossTenantService;
        private readonly ICrossTenantAuthorizationService _authService;
        private readonly ICurrentUserService _currentUserService;

        public ResendUserInvitationInTenantCommandHandler(
            ICrossTenantService crossTenantService,
            ICrossTenantAuthorizationService authService,
            ICurrentUserService currentUserService)
        {
            _crossTenantService = crossTenantService;
            _authService = authService;
            _currentUserService = currentUserService;
        }

        public async Task<IResponse> Handle(ResendUserInvitationInTenantCommand request, CancellationToken cancellationToken)
        {
            var userIdString = _currentUserService.GetUserId();
            if (!Guid.TryParse(userIdString, out var currentUserId))
            {
                var resultResponse = new ResultResponse();
                resultResponse.AddErrorMessage("Invalid user ID");
                throw new UnauthorizedException(resultResponse);
            }

            var isSystemAdmin = await _authService.IsSystemAdminAsync(currentUserId);
            var isSupportUser = await _authService.IsSupportUserAsync(currentUserId);
            var isTenantOwner = await _authService.IsTenantOwnerAsync(currentUserId);
            var isTenantManager = await _authService.IsTenantManagerAsync(currentUserId);
            var currentUserTenantId = _currentUserService.GetUserTenant() ?? string.Empty;

            var allowed = isSystemAdmin || isSupportUser
                || ((isTenantOwner || isTenantManager) && !string.IsNullOrEmpty(currentUserTenantId) && string.Equals(currentUserTenantId, request.TenantId, StringComparison.OrdinalIgnoreCase));

            if (!allowed)
            {
                var resultResponse = new ResultResponse();
                resultResponse.AddErrorMessage("You do not have permission to resend invitations in this tenant.");
                throw new ForbiddenException(resultResponse);
            }

            return await _crossTenantService.ExecuteInTenantContextAsync(request.TenantId,
                async serviceProvider =>
                {
                    var activationService = serviceProvider.GetRequiredService<IUserActivationService>();
                    var inviteResult = await activationService.ResendInvitationAsync(request.UserId, cancellationToken);
                    var response = new SingleResponse<InviteUserResponse>();
                    response.Data = inviteResult;
                    response.AddSuccessMessage("Invitation resent successfully.", "user.invitation_resent_successfully");
                    return response;
                });
        }
    }
}


