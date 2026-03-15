using InteractiveLeads.Application.Feature.Users;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Application.Feature.CrossTenant.Commands
{
    /// <summary>
    /// Command for changing user status in a specific tenant - available for SysAdmin and Support.
    /// </summary>
    /// <remarks>
    /// This command implements the CQRS pattern for cross-tenant user status change operations.
    /// It encapsulates the tenant context switching logic.
    /// </remarks>
    public sealed class ChangeUserStatusInTenantCommand : IRequest<IResponse>, IValidate
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
        /// Gets or sets the user status change data.
        /// </summary>
        public ChangeUserStatusRequest ChangeUserStatus { get; set; } = new();
    }

    /// <summary>
    /// Handler for processing ChangeUserStatusInTenantCommand requests.
    /// </summary>
    /// <remarks>
    /// Executes the user status change operation in the specified tenant context.
    /// </remarks>
    public sealed class ChangeUserStatusInTenantCommandHandler : IRequestHandler<ChangeUserStatusInTenantCommand, IResponse>
    {
        private readonly ICrossTenantService _crossTenantService;

        /// <summary>
        /// Initializes a new instance of the ChangeUserStatusInTenantCommandHandler class.
        /// </summary>
        /// <param name="crossTenantService">The cross-tenant service for context switching.</param>
        public ChangeUserStatusInTenantCommandHandler(ICrossTenantService crossTenantService)
        {
            _crossTenantService = crossTenantService;
        }

        /// <summary>
        /// Handles the ChangeUserStatusInTenantCommand request and changes the user status in the specified tenant.
        /// </summary>
        /// <param name="request">The command request containing the tenant ID, user ID, and status change data.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A wrapped response containing the result of the status change operation.</returns>
        public async Task<IResponse> Handle(ChangeUserStatusInTenantCommand request, CancellationToken cancellationToken)
        {
            return await _crossTenantService.ExecuteInTenantContextAsync(request.TenantId,
                async (serviceProvider) => 
                {
                    var userService = serviceProvider.GetRequiredService<IUserService>();
                    return await userService.ActivateOrDeactivateAsync(request.ChangeUserStatus.UserId, request.ChangeUserStatus.Activation);
                });
        }
    }
}
