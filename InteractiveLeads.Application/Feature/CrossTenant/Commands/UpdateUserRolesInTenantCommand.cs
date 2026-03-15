using InteractiveLeads.Application.Feature.Users;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Application.Feature.CrossTenant.Commands
{
    /// <summary>
    /// Command for updating user roles in a specific tenant - available for SysAdmin and Support.
    /// </summary>
    /// <remarks>
    /// This command implements the CQRS pattern for cross-tenant user role update operations.
    /// It encapsulates the tenant context switching logic.
    /// </remarks>
    public sealed class UpdateUserRolesInTenantCommand : IRequest<IResponse>, IValidate
    {
        /// <summary>
        /// Gets or sets the ID of the tenant.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the user to update.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the list of roles to be assigned to the user.
        /// </summary>
        public UserRolesRequest UserRolesRequest { get; set; } = new();
    }

    /// <summary>
    /// Handler for processing UpdateUserRolesInTenantCommand requests.
    /// </summary>
    /// <remarks>
    /// Executes the user role update operation in the specified tenant context.
    /// </remarks>
    public sealed class UpdateUserRolesInTenantCommandHandler : IRequestHandler<UpdateUserRolesInTenantCommand, IResponse>
    {
        private readonly ICrossTenantService _crossTenantService;

        /// <summary>
        /// Initializes a new instance of the UpdateUserRolesInTenantCommandHandler class.
        /// </summary>
        /// <param name="crossTenantService">The cross-tenant service for context switching.</param>
        public UpdateUserRolesInTenantCommandHandler(ICrossTenantService crossTenantService)
        {
            _crossTenantService = crossTenantService;
        }

        /// <summary>
        /// Handles the UpdateUserRolesInTenantCommand request and updates user roles in the specified tenant.
        /// </summary>
        /// <param name="request">The command request containing the tenant ID, user ID, and roles data.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A wrapped response containing the result of the roles update operation.</returns>
        public async Task<IResponse> Handle(UpdateUserRolesInTenantCommand request, CancellationToken cancellationToken)
        {
            return await _crossTenantService.ExecuteInTenantContextAsync(request.TenantId,
                async (serviceProvider) => 
                {
                    var userService = serviceProvider.GetRequiredService<IUserService>();
                    return await userService.AssignRolesAsync(request.UserId, request.UserRolesRequest);
                });
        }
    }
}
