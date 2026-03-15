using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Users;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Application.Feature.CrossTenant.Commands
{
    /// <summary>
    /// Command for creating a new user in a specific tenant - available for SysAdmin and Support.
    /// </summary>
    /// <remarks>
    /// This command implements the CQRS pattern for cross-tenant user creation operations.
    /// It encapsulates the tenant context switching logic and authorization validation.
    /// </remarks>
    public sealed class CreateUserInTenantCommand : IRequest<IResponse>, IValidate
    {
        /// <summary>
        /// Gets or sets the ID of the tenant to create the user in.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user data to be created.
        /// </summary>
        public CreateUserRequest CreateUser { get; set; } = new();
    }

    /// <summary>
    /// Handler for processing CreateUserInTenantCommand requests.
    /// </summary>
    /// <remarks>
    /// Executes the user creation operation in the specified tenant context.
    /// Validates that SysAdmin and Support users can create users in other tenants.
    /// </remarks>
    public sealed class CreateUserInTenantCommandHandler : IRequestHandler<CreateUserInTenantCommand, IResponse>
    {
        private readonly ICrossTenantService _crossTenantService;
        private readonly ICrossTenantAuthorizationService _authService;
        private readonly ICurrentUserService _currentUserService;

        /// <summary>
        /// Initializes a new instance of the CreateUserInTenantCommandHandler class.
        /// </summary>
        /// <param name="crossTenantService">The cross-tenant service for context switching.</param>
        /// <param name="authService">The authorization service for cross-tenant operations.</param>
        /// <param name="currentUserService">The current user service.</param>
        public CreateUserInTenantCommandHandler(
            ICrossTenantService crossTenantService,
            ICrossTenantAuthorizationService authService,
            ICurrentUserService currentUserService)
        {
            _crossTenantService = crossTenantService;
            _authService = authService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Handles the CreateUserInTenantCommand request and creates a user in the specified tenant.
        /// </summary>
        /// <param name="request">The command request containing the tenant ID and user data.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A wrapped response containing the result of the user creation operation.</returns>
        public async Task<IResponse> Handle(CreateUserInTenantCommand request, CancellationToken cancellationToken)
        {
            var userIdString = _currentUserService.GetUserId();
            if (!Guid.TryParse(userIdString, out var userId))
            {
                ResultResponse resultResponse = new();
                resultResponse.AddErrorMessage("Invalid user ID");

                throw new UnauthorizedException(resultResponse);
            }
            
            var isSystemAdmin = await _authService.IsSystemAdminAsync(userId);
            var isSupportUser = await _authService.IsSupportUserAsync(userId);
            
            if (!isSystemAdmin && !isSupportUser)
            {
                ResultResponse resultResponse = new();
                resultResponse.AddErrorMessage("Only system administrators and support users can create users from other tenants");

                throw new ForbiddenException(resultResponse);
            }

            return await _crossTenantService.ExecuteInTenantContextAsync(request.TenantId,
                async (serviceProvider) => 
                {
                    var userService = serviceProvider.GetRequiredService<IUserService>();
                    return await userService.CreateAsync(request.CreateUser);
                });
        }
    }
}
