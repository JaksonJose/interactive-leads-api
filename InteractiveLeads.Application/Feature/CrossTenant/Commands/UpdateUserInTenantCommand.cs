using InteractiveLeads.Application.Feature.Users;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Application.Feature.CrossTenant.Commands
{
    /// <summary>
    /// Command for updating a user in a specific tenant - available for SysAdmin and Support.
    /// </summary>
    /// <remarks>
    /// This command implements the CQRS pattern for cross-tenant user update operations.
    /// It encapsulates the tenant context switching logic.
    /// </remarks>
    public sealed class UpdateUserInTenantCommand : IRequest<IResponse>, IValidate
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
        /// Gets or sets the updated user data.
        /// </summary>
        public UpdateUserRequest UpdateUser { get; set; } = new();
    }

    /// <summary>
    /// Handler for processing UpdateUserInTenantCommand requests.
    /// </summary>
    /// <remarks>
    /// Executes the user update operation in the specified tenant context.
    /// </remarks>
    public sealed class UpdateUserInTenantCommandHandler : IRequestHandler<UpdateUserInTenantCommand, IResponse>
    {
        private readonly ICrossTenantService _crossTenantService;

        /// <summary>
        /// Initializes a new instance of the UpdateUserInTenantCommandHandler class.
        /// </summary>
        /// <param name="crossTenantService">The cross-tenant service for context switching.</param>
        public UpdateUserInTenantCommandHandler(ICrossTenantService crossTenantService)
        {
            _crossTenantService = crossTenantService;
        }

        /// <summary>
        /// Handles the UpdateUserInTenantCommand request and updates the user in the specified tenant.
        /// </summary>
        /// <param name="request">The command request containing the tenant ID, user ID, and update data.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A wrapped response containing the result of the user update operation.</returns>
        public async Task<IResponse> Handle(UpdateUserInTenantCommand request, CancellationToken cancellationToken)
        {
            return await _crossTenantService.ExecuteInTenantContextAsync(request.TenantId,
                async (serviceProvider) => 
                {
                    var userService = serviceProvider.GetRequiredService<IUserService>();
                    return await userService.UpdateAsync(request.UpdateUser);
                });
        }
    }
}
