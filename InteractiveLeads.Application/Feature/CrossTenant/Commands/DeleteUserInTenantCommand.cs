using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Application.Feature.CrossTenant.Commands
{
    /// <summary>
    /// Command for deleting a user from a specific tenant - SysAdmin only.
    /// </summary>
    /// <remarks>
    /// This command implements the CQRS pattern for cross-tenant user deletion operations.
    /// It encapsulates the tenant context switching logic and authorization validation.
    /// </remarks>
    public sealed class DeleteUserInTenantCommand : IRequest<IResponse>, IValidate
    {
        /// <summary>
        /// Gets or sets the ID of the tenant.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the user to delete.
        /// </summary>
        public Guid UserId { get; set; }
    }

    /// <summary>
    /// Handler for processing DeleteUserInTenantCommand requests.
    /// </summary>
    /// <remarks>
    /// Executes the user deletion operation in the specified tenant context.
    /// Validates that only SysAdmin users can delete users from other tenants.
    /// </remarks>
    public sealed class DeleteUserInTenantCommandHandler : IRequestHandler<DeleteUserInTenantCommand, IResponse>
    {
        private readonly ICrossTenantService _crossTenantService;
        private readonly ICrossTenantAuthorizationService _authService;
        private readonly ICurrentUserService _currentUserService;

        /// <summary>
        /// Initializes a new instance of the DeleteUserInTenantCommandHandler class.
        /// </summary>
        /// <param name="crossTenantService">The cross-tenant service for context switching.</param>
        /// <param name="authService">The authorization service for cross-tenant operations.</param>
        /// <param name="currentUserService">The current user service.</param>
        public DeleteUserInTenantCommandHandler(
            ICrossTenantService crossTenantService,
            ICrossTenantAuthorizationService authService,
            ICurrentUserService currentUserService)
        {
            _crossTenantService = crossTenantService;
            _authService = authService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Handles the DeleteUserInTenantCommand request and deletes the user from the specified tenant.
        /// </summary>
        /// <param name="request">The command request containing the tenant ID and user ID.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A wrapped response containing the result of the user deletion operation.</returns>
        public async Task<IResponse> Handle(DeleteUserInTenantCommand request, CancellationToken cancellationToken)
        {
            // Additional verification: only SysAdmin can delete users
            var userIdString = _currentUserService.GetUserId();
            if (!Guid.TryParse(userIdString, out var userId))
            {
                ResultResponse resultResponse = new();
                resultResponse.AddErrorMessage("Invalid user ID");

                throw new UnauthorizedException(resultResponse);
            }
            
            if (!await _authService.IsSystemAdminAsync(userId))
            {
                ResultResponse resultResponse = new();
                resultResponse.AddErrorMessage("Only system administrators can delete users from other tenants");

                throw new ForbiddenException(resultResponse);
            }

            return await _crossTenantService.ExecuteInTenantContextAsync(request.TenantId,
                async (serviceProvider) => 
                {
                    var userService = serviceProvider.GetRequiredService<IUserService>();
                    return await userService.DeleteAsync(request.UserId);
                });
        }
    }
}
