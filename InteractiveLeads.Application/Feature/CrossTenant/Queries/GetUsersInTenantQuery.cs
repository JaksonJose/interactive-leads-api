using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Application.Feature.CrossTenant.Queries
{
    /// <summary>
    /// Query for retrieving all users in a specific tenant - available for SysAdmin and Support.
    /// </summary>
    /// <remarks>
    /// This query implements the CQRS pattern for cross-tenant user retrieval operations.
    /// It encapsulates the tenant context switching logic.
    /// </remarks>
    public sealed class GetUsersInTenantQuery : IApplicationRequest<IResponse>
    {
        /// <summary>
        /// Gets or sets the ID of the tenant to retrieve users from.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handler for processing GetUsersInTenantQuery requests.
    /// </summary>
    /// <remarks>
    /// Executes the user retrieval operation in the specified tenant context.
    /// </remarks>
    public sealed class GetUsersInTenantQueryHandler : IApplicationRequestHandler<GetUsersInTenantQuery, IResponse>
    {
        private readonly ICrossTenantService _crossTenantService;

        /// <summary>
        /// Initializes a new instance of the GetUsersInTenantQueryHandler class.
        /// </summary>
        /// <param name="crossTenantService">The cross-tenant service for context switching.</param>
        public GetUsersInTenantQueryHandler(ICrossTenantService crossTenantService)
        {
            _crossTenantService = crossTenantService;
        }

        /// <summary>
        /// Handles the GetUsersInTenantQuery request and retrieves users from the specified tenant.
        /// </summary>
        /// <param name="request">The query request containing the tenant ID.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A wrapped response containing the list of users from the specified tenant.</returns>
        public async Task<IResponse> Handle(GetUsersInTenantQuery request, CancellationToken cancellationToken)
        {
            return await _crossTenantService.ExecuteInTenantContextAsync(request.TenantId,
                async (serviceProvider) => 
                {
                    var userService = serviceProvider.GetRequiredService<IUserService>();
                    return await userService.GetAllAsync(cancellationToken);
                });
        }
    }
}

