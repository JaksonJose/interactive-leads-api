using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Tenancy.Commands
{
    /// <summary>
    /// Command for deactivating an existing tenant.
    /// </summary>
    /// <remarks>
    /// This command implements the CQRS pattern for tenant deactivation operations.
    /// Deactivating a tenant disables their access to the system.
    /// </remarks>
    public sealed class DeactivateTenantCommand : IRequest<IResponse>, IValidate
    {
        /// <summary>
        /// Gets or sets the unique identifier of the tenant to deactivate.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handler for processing DeactivateTenantCommand requests.
    /// </summary>
    /// <remarks>
    /// Deactivates the specified tenant via ITenantService.
    /// </remarks>
    public sealed class DeactivateTenantCommandHandler : IRequestHandler<DeactivateTenantCommand, IResponse>
    {
        private readonly ITenantService _tenantService;
        private readonly ICrossTenantAuthorizationService _authService;
        private readonly ICurrentUserService _currentUserService;

        /// <summary>
        /// Initializes a new instance of the DeactivateTenantCommandHandler class.
        /// </summary>
        public DeactivateTenantCommandHandler(
            ITenantService tenantService,
            ICrossTenantAuthorizationService authService,
            ICurrentUserService currentUserService)
        {
            _tenantService = tenantService;
            _authService = authService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Handles the DeactivateTenantCommand request and deactivates the tenant. Only SysAdmin can deactivate tenants.
        /// </summary>
        public async Task<IResponse> Handle(DeactivateTenantCommand request, CancellationToken cancellationToken)
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
                resultResponse.AddErrorMessage("Only system administrators can deactivate tenants");
                throw new ForbiddenException(resultResponse);
            }

            return await _tenantService.DeactivateAsync(request.TenantId, cancellationToken);
        }
    }
}
