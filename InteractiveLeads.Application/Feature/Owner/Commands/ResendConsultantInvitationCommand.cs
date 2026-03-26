using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Activation;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Application.Feature.Owner.Commands
{
    /// <summary>
    /// Command to resend activation invitation for a consultant in the current owner's tenant.
    /// </summary>
    public sealed class ResendConsultantInvitationCommand : IApplicationRequest<IResponse>
    {
        public string TenantId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
    }

    public sealed class ResendConsultantInvitationCommandHandler : IApplicationRequestHandler<ResendConsultantInvitationCommand, IResponse>
    {
        private readonly ICrossTenantService _crossTenantService;
        private readonly ICurrentUserService _currentUserService;

        public ResendConsultantInvitationCommandHandler(
            ICrossTenantService crossTenantService,
            ICurrentUserService currentUserService)
        {
            _crossTenantService = crossTenantService;
            _currentUserService = currentUserService;
        }

        public async Task<IResponse> Handle(ResendConsultantInvitationCommand request, CancellationToken cancellationToken)
        {
            var currentTenantId = _currentUserService.GetUserTenant() ?? string.Empty;
            if (string.IsNullOrEmpty(currentTenantId) || !string.Equals(currentTenantId, request.TenantId, StringComparison.OrdinalIgnoreCase))
            {
                var resultResponse = new ResultResponse();
                resultResponse.AddErrorMessage("Tenant context is required or does not match.");
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


