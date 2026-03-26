using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace InteractiveLeads.Application.Feature.Tenancy.Commands
{
    /// <summary>
    /// Command for creating a new tenant in the system.
    /// </summary>
    /// <remarks>
    /// This command implements the CQRS pattern for tenant creation operations.
    /// </remarks>
    public sealed class CreateTenantCommand : IApplicationRequest<IResponse>, IValidate
    {
        /// <summary>
        /// Gets or sets the tenant creation request containing tenant details.
        /// </summary>
        public CreateTenantRequest CreateTenant { get; set; } = new();
    }

    /// <summary>
    /// Handler for processing CreateTenantCommand requests.
    /// </summary>
    /// <remarks>
    /// Creates a new tenant via ITenantService and returns the tenant identifier.
    /// </remarks>
    public sealed class CreateTenantCommandHandler : IApplicationRequestHandler<CreateTenantCommand, IResponse>
    {
        private readonly ITenantService _tenantService;
        private readonly ICrossTenantAuthorizationService _authService;
        private readonly ICurrentUserService _currentUserService;

        /// <summary>
        /// Initializes a new instance of the CreateTenantCommandHandler class.
        /// </summary>
        public CreateTenantCommandHandler(
            ITenantService tenantService,
            ICrossTenantAuthorizationService authService,
            ICurrentUserService currentUserService)
        {
            _tenantService = tenantService;
            _authService = authService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Handles the CreateTenantCommand request and creates the tenant. Only SysAdmin can create tenants.
        /// </summary>
        public async Task<IResponse> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
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
                resultResponse.AddErrorMessage("Only system administrators can create tenants");
                throw new ForbiddenException(resultResponse);
            }

            return await _tenantService.CreateTenantAsync(request.CreateTenant, cancellationToken);
        }
    }
}

