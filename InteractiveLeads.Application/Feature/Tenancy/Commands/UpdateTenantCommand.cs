using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace InteractiveLeads.Application.Feature.Tenancy.Commands
{
    /// <summary>
    /// Command for updating an existing tenant in the system.
    /// </summary>
    /// <remarks>
    /// This command implements the CQRS pattern for tenant update operations.
    /// </remarks>
    public sealed class UpdateTenantCommand : IApplicationRequest<IResponse>, IValidate
    {
        /// <summary>
        /// Gets or sets the tenant update request containing tenant details.
        /// </summary>
        public UpdateTenantRequest UpdateTenant { get; set; } = new();
    }

    /// <summary>
    /// Handler for processing UpdateTenantCommand requests.
    /// </summary>
    /// <remarks>
    /// Updates an existing tenant via ITenantService and returns the result.
    /// </remarks>
    public sealed class UpdateTenantCommandHandler : IApplicationRequestHandler<UpdateTenantCommand, IResponse>
    {
        private readonly ITenantService _tenantService;
        private readonly ICrossTenantAuthorizationService _authService;
        private readonly ICurrentUserService _currentUserService;

        /// <summary>
        /// Initializes a new instance of the UpdateTenantCommandHandler class.
        /// </summary>
        public UpdateTenantCommandHandler(
            ITenantService tenantService,
            ICrossTenantAuthorizationService authService,
            ICurrentUserService currentUserService)
        {
            _tenantService = tenantService;
            _authService = authService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Handles the UpdateTenantCommand request and updates the tenant. Only SysAdmin can update tenants.
        /// </summary>
        public async Task<IResponse> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
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
                resultResponse.AddErrorMessage("Only system administrators can update tenants");
                throw new ForbiddenException(resultResponse);
            }

            return await _tenantService.UpdateTenantAsync(request.UpdateTenant, cancellationToken);
        }
    }
}


