using InteractiveLeads.Application.Feature.Activation;
using InteractiveLeads.Application.Feature.CrossTenant;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Tenancy.Commands
{
    /// <summary>
    /// Command to resend activation invitation for the tenant owner.
    /// </summary>
    public sealed class ResendTenantOwnerActivationCommand : IRequest<IResponse>, IValidate
    {
        public string TenantId { get; set; } = string.Empty;
        public Guid OwnerUserId { get; set; }
    }

    public sealed class ResendTenantOwnerActivationCommandHandler : IRequestHandler<ResendTenantOwnerActivationCommand, IResponse>
    {
        private readonly IUserActivationService _activationService;
        private readonly ICrossTenantService _crossTenantService;

        public ResendTenantOwnerActivationCommandHandler(
            IUserActivationService activationService,
            ICrossTenantService crossTenantService)
        {
            _activationService = activationService;
            _crossTenantService = crossTenantService;
        }

        public async Task<IResponse> Handle(ResendTenantOwnerActivationCommand request, CancellationToken cancellationToken)
        {
            var inviteResponse = await _activationService.ResendInvitationAsync(request.OwnerUserId, cancellationToken);

            var response = new SingleResponse<InviteUserResponse>();
            response.Data = inviteResponse;
            response.AddSuccessMessage("Invitation resent successfully.", "user.invitation_resent_successfully");
            return response;
        }
    }
}

