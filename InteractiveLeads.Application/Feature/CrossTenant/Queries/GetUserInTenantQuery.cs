using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Application.Feature.CrossTenant.Queries
{
    /// <summary>
    /// Query for retrieving a specific user from a tenant - available for SysAdmin and Support.
    /// </summary>
    /// <remarks>
    /// This query implements the CQRS pattern for cross-tenant user retrieval operations.
    /// It encapsulates the tenant context switching logic.
    /// </remarks>
    public sealed class GetUserInTenantQuery : IRequest<IResponse>
    {
        /// <summary>
        /// Gets or sets the ID of the tenant.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the user to retrieve.
        /// </summary>
        public Guid UserId { get; set; }
    }

    /// <summary>
    /// Handler for processing GetUserInTenantQuery requests.
    /// </summary>
    /// <remarks>
    /// Executes the user retrieval operation in the specified tenant context.
    /// </remarks>
    public sealed class GetUserInTenantQueryHandler : IRequestHandler<GetUserInTenantQuery, IResponse>
    {
        private readonly ICrossTenantService _crossTenantService;
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the GetUserInTenantQueryHandler class.
        /// </summary>
        /// <param name="crossTenantService">The cross-tenant service for context switching.</param>
        /// <param name="mediator">The mediator for sending internal queries.</param>
        public GetUserInTenantQueryHandler(ICrossTenantService crossTenantService, IMediator mediator)
        {
            _crossTenantService = crossTenantService;
            _mediator = mediator;
        }

        /// <summary>
        /// Handles the GetUserInTenantQuery request and retrieves the specified user from the tenant.
        /// </summary>
        /// <param name="request">The query request containing the tenant ID and user ID.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A wrapped response containing the user data from the specified tenant.</returns>
        public async Task<IResponse> Handle(GetUserInTenantQuery request, CancellationToken cancellationToken)
        {
            return await _crossTenantService.ExecuteInTenantContextAsync(request.TenantId,
                async (serviceProvider) => 
                {
                    var userService = serviceProvider.GetRequiredService<IUserService>();
                    return await userService.GetByIdAsync(request.UserId, cancellationToken);
                });
        }
    }
}
