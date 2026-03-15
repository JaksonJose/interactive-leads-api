using InteractiveLeads.Application.Feature.CrossTenant;
using InteractiveLeads.Application.Feature.Tenancy;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Application.Feature.CrossTenant.Queries
{
    /// <summary>
    /// Query for retrieving a specific tenant with its associated user - available for SysAdmin and Support.
    /// </summary>
    /// <remarks>
    /// This query implements the CQRS pattern for cross-tenant operations.
    /// It retrieves tenant information by ID and then finds the associated user by the tenant's email.
    /// </remarks>
    public sealed class GetTenantWithUserQuery : IRequest<IResponse>, IValidate
    {
        /// <summary>
        /// Gets or sets the ID of the tenant to retrieve with its associated user.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handler for processing GetTenantWithUserQuery requests.
    /// </summary>
    /// <remarks>
    /// Retrieves tenant information and then finds the user associated with the tenant's email.
    /// </remarks>
    public sealed class GetTenantWithUserQueryHandler : IRequestHandler<GetTenantWithUserQuery, IResponse>
    {
        private readonly ITenantService _tenantService;
        private readonly ICrossTenantService _crossTenantService;

        /// <summary>
        /// Initializes a new instance of the GetTenantWithUserQueryHandler class.
        /// </summary>
        /// <param name="tenantService">The tenant service for retrieving tenant information.</param>
        /// <param name="crossTenantService">The cross-tenant service for context switching.</param>
        public GetTenantWithUserQueryHandler(ITenantService tenantService, ICrossTenantService crossTenantService)
        {
            _tenantService = tenantService;
            _crossTenantService = crossTenantService;
        }

        /// <summary>
        /// Handles the GetTenantWithUserQuery request and retrieves tenant with its associated user.
        /// </summary>
        /// <param name="request">The query request containing the tenant ID.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A wrapped response containing the tenant data with associated user.</returns>
        public async Task<IResponse> Handle(GetTenantWithUserQuery request, CancellationToken cancellationToken)
        {
            var tenantResponse = await _tenantService.GetTenantsByIdAsync(request.TenantId, cancellationToken);            
            if (tenantResponse.HasAnyErrorMessage)
            {
                return tenantResponse;
            }

            var usersResponse = await _crossTenantService.ExecuteInTenantContextAsync(request.TenantId,
                async (serviceProvider) => 
                {
                    var userService = serviceProvider.GetRequiredService<IUserService>();
                    return await userService.GetByEmailAsync(tenantResponse.Data!.Email, cancellationToken);
                });

            tenantResponse.Data!.User = usersResponse.Data!;

            return tenantResponse;
        }
    }
}