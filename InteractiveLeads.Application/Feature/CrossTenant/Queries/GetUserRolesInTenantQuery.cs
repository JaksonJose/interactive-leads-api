using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Application.Feature.CrossTenant.Queries
{
    /// <summary>
    /// Query for retrieving user roles in a specific tenant - available for SysAdmin and Support.
    /// </summary>
    /// <remarks>
    /// This query implements the CQRS pattern for cross-tenant user role retrieval operations.
    /// It encapsulates the tenant context switching logic.
    /// </remarks>
    public sealed class GetUserRolesInTenantQuery : IRequest<IResponse>
    {
        /// <summary>
        /// Gets or sets the ID of the tenant.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the user to get roles for.
        /// </summary>
        public Guid UserId { get; set; }
    }

    /// <summary>
    /// Handler for processing GetUserRolesInTenantQuery requests.
    /// </summary>
    /// <remarks>
    /// Executes the user role retrieval operation in the specified tenant context.
    /// </remarks>
    public sealed class GetUserRolesInTenantQueryHandler : IRequestHandler<GetUserRolesInTenantQuery, IResponse>
    {
        private readonly ICrossTenantService _crossTenantService;

        /// <summary>
        /// Initializes a new instance of the GetUserRolesInTenantQueryHandler class.
        /// </summary>
        /// <param name="crossTenantService">The cross-tenant service for context switching.</param>
        public GetUserRolesInTenantQueryHandler(ICrossTenantService crossTenantService)
        {
            _crossTenantService = crossTenantService;
        }

        /// <summary>
        /// Handles the GetUserRolesInTenantQuery request and retrieves user roles from the tenant.
        /// </summary>
        /// <param name="request">The query request containing the tenant ID and user ID.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A wrapped response containing the user roles from the specified tenant.</returns>
        public async Task<IResponse> Handle(GetUserRolesInTenantQuery request, CancellationToken cancellationToken)
        {
            return await _crossTenantService.ExecuteInTenantContextAsync(request.TenantId,
                async (serviceProvider) => 
                {
                    var userService = serviceProvider.GetRequiredService<IUserService>();
                    return await userService.GetUserRolesAsync(request.UserId, cancellationToken);
                });
        }
    }
}
