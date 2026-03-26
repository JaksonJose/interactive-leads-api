using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Activation;
using InteractiveLeads.Application.Feature.Users;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Application.Feature.Owner.Commands
{
    /// <summary>
    /// Command to invite a consultant in the current owner's tenant.
    /// </summary>
    public sealed class InviteConsultantCommand : IApplicationRequest<IResponse>
    {
        public string TenantId { get; set; } = string.Empty;
        public InviteUserRequest InviteUser { get; set; } = new();
    }

    /// <summary>
    /// Handler for InviteConsultantCommand. Ensures caller is Owner and tenant matches.
    /// </summary>
    public sealed class InviteConsultantCommandHandler : IApplicationRequestHandler<InviteConsultantCommand, IResponse>
    {
        private readonly ICrossTenantService _crossTenantService;
        private readonly ICurrentUserService _currentUserService;

        public InviteConsultantCommandHandler(
            ICrossTenantService crossTenantService,
            ICurrentUserService currentUserService)
        {
            _crossTenantService = crossTenantService;
            _currentUserService = currentUserService;
        }

        public async Task<IResponse> Handle(InviteConsultantCommand request, CancellationToken cancellationToken)
        {
            var currentTenantId = _currentUserService.GetUserTenant() ?? string.Empty;
            if (string.IsNullOrEmpty(currentTenantId) || !string.Equals(currentTenantId, request.TenantId, StringComparison.OrdinalIgnoreCase))
            {
                var resultResponse = new ResultResponse();
                resultResponse.AddErrorMessage("Tenant context is required or does not match.");
                throw new ForbiddenException(resultResponse);
            }

            return await _crossTenantService.ExecuteInTenantContextAsync(request.TenantId,
                async (serviceProvider) =>
                {
                    var activationService = serviceProvider.GetRequiredService<IUserActivationService>();
                    var inviteResult = await activationService.CreateInvitationAsync(request.InviteUser, cancellationToken);
                    var response = new SingleResponse<InviteUserResponse>();
                    response.Data = inviteResult;
                    response.AddSuccessMessage("Consultant invited successfully.", "user.invited_successfully");
                    return response;
                });
        }
    }
}

