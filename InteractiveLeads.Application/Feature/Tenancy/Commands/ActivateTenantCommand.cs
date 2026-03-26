using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace InteractiveLeads.Application.Feature.Tenancy.Commands
{
    /// <summary>
    /// Command for activating an existing tenant.
    /// </summary>
    /// <remarks>
    /// This command implements the CQRS pattern for tenant activation operations.
    /// Activating a tenant enables their access to the system.
    /// </remarks>
    public sealed class ActivateTenantCommand : IApplicationRequest<IResponse>, IValidate
    {
        /// <summary>
        /// Gets or sets the unique identifier of the tenant to activate.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handler for processing ActivateTenantCommand requests.
    /// </summary>
    /// <remarks>
    /// Activates the specified tenant via ITenantService.
    /// </remarks>
    public sealed class ActivateTenantCommandHandler : IApplicationRequestHandler<ActivateTenantCommand, IResponse>
    {
        private readonly ITenantService _tenantService;
        private readonly ICrossTenantAuthorizationService _authService;
        private readonly ICurrentUserService _currentUserService;

        /// <summary>
        /// Initializes a new instance of the ActivateTenantCommandHandler class.
        /// </summary>
        public ActivateTenantCommandHandler(
            ITenantService tenantService,
            ICrossTenantAuthorizationService authService,
            ICurrentUserService currentUserService)
        {
            _tenantService = tenantService;
            _authService = authService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Handles the ActivateTenantCommand request and activates the tenant. Only SysAdmin can activate tenants.
        /// </summary>
        public async Task<IResponse> Handle(ActivateTenantCommand request, CancellationToken cancellationToken)
        {
            var userIdString = _currentUserService.GetUserId();
            if (!Guid.TryParse(userIdString, out var userId))
            {
                var resultResponse = new ResultResponse();
                resultResponse.AddErrorMessage("Invalid user ID");
                throw new UnauthorizedException(resultResponse);
            }

            if (!await _authService.IsSystemAdminAsync(userId))
            {
                var resultResponse = new ResultResponse();
                resultResponse.AddErrorMessage("Only system administrators can activate tenants");
                throw new ForbiddenException(resultResponse);
            }

            return await _tenantService.ActivateAsync(request.TenantId, cancellationToken);
        }
    }
}

