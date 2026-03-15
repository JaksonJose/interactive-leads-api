using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Application.Feature.CrossTenant.Queries
{
    /// <summary>
    /// Query for retrieving all available roles in a specific tenant - available for SysAdmin and Support.
    /// </summary>
    /// <remarks>
    /// This query implements the CQRS pattern for cross-tenant role retrieval operations.
    /// It encapsulates the tenant context switching logic.
    /// </remarks>
    public sealed class GetRolesInTenantQuery : IRequest<IResponse>
    {
        /// <summary>
        /// Gets or sets the ID of the tenant to retrieve roles from.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handler for processing GetRolesInTenantQuery requests.
    /// </summary>
    /// <remarks>
    /// Executes the role retrieval operation in the specified tenant context.
    /// </remarks>
    public sealed class GetRolesInTenantQueryHandler : IRequestHandler<GetRolesInTenantQuery, IResponse>
    {
        private readonly ICrossTenantService _crossTenantService;

        /// <summary>
        /// Initializes a new instance of the GetRolesInTenantQueryHandler class.
        /// </summary>
        /// <param name="crossTenantService">The cross-tenant service for context switching.</param>
        public GetRolesInTenantQueryHandler(ICrossTenantService crossTenantService)
        {
            _crossTenantService = crossTenantService;
        }

        /// <summary>
        /// Handles the GetRolesInTenantQuery request and retrieves roles from the specified tenant.
        /// </summary>
        /// <param name="request">The query request containing the tenant ID.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A wrapped response containing the list of roles from the specified tenant.</returns>
        public async Task<IResponse> Handle(GetRolesInTenantQuery request, CancellationToken cancellationToken)
        {
            return await _crossTenantService.ExecuteInTenantContextAsync(request.TenantId,
                async (serviceProvider) => 
                {
                    var roleService = serviceProvider.GetRequiredService<IRoleService>();
                    return await roleService.GetAllAsync(cancellationToken);
                });
        }
    }
}

