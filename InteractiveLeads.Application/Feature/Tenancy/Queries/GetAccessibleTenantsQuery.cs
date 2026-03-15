using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Models;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Tenancy.Queries
{
    /// <summary>
    /// Query for retrieving accessible tenants for the current user.
    /// </summary>
    /// <remarks>
    /// This query implements the CQRS pattern for retrieving tenant records that the current user has access to.
    /// For cross-tenant users, returns all tenants. For regular users, returns only their tenant.
    /// </remarks>
    public sealed class GetAccessibleTenantsQuery : IRequest<IResponse>
    {
        /// <summary>
        /// Gets or sets the pagination parameters for the query.
        /// </summary>
        public PaginationRequest Pagination { get; set; } = new();
    }

    /// <summary>
    /// Handler for processing GetAccessibleTenantsQuery requests.
    /// </summary>
    /// <remarks>
    /// Retrieves accessible tenants based on user permissions via ICrossTenantAuthorizationService and ITenantService.
    /// </remarks>
    public sealed class GetAccessibleTenantsQueryHandler : IRequestHandler<GetAccessibleTenantsQuery, IResponse>
    {
        private readonly ICrossTenantAuthorizationService _authService;
        private readonly ITenantService _tenantService;
        private readonly ICurrentUserService _currentUserService;

        /// <summary>
        /// Initializes a new instance of the GetAccessibleTenantsQueryHandler class.
        /// </summary>
        /// <param name="authService">The cross-tenant authorization service for checking user permissions.</param>
        /// <param name="tenantService">The tenant service for managing tenant operations.</param>
        /// <param name="currentUserService">The current user service for getting user information.</param>
        public GetAccessibleTenantsQueryHandler(
            ICrossTenantAuthorizationService authService,
            ITenantService tenantService,
            ICurrentUserService currentUserService)
        {
            _authService = authService;
            _tenantService = tenantService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Handles the GetAccessibleTenantsQuery request and retrieves accessible tenants.
        /// </summary>
        /// <param name="request">The query request containing pagination parameters.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A wrapped response containing accessible tenants.</returns>
        /// <exception cref="UnauthorizedException">Thrown when user ID is invalid.</exception>
        /// <exception cref="ForbiddenException">Thrown when user lacks cross-tenant access.</exception>
        public async Task<IResponse> Handle(GetAccessibleTenantsQuery request, CancellationToken cancellationToken)
        {
            var userIdString = _currentUserService.GetUserId();
            if (!Guid.TryParse(userIdString, out var userId))
            {
                ResultResponse resultResponse = new();
                resultResponse.AddErrorMessage("Invalid user ID", "general.unauthorized");

                throw new UnauthorizedException(resultResponse);
            }

            // Check if user has cross-tenant access
            var hasAllTenantsAccess = await _authService.HasAllTenantsAccessAsync(userId);
            if (!hasAllTenantsAccess)
            {
                ResultResponse resultResponse = new();
                resultResponse.AddErrorMessage("User does not have cross-tenant access", "general.forbidden");

                throw new ForbiddenException(resultResponse);
            }

            // For cross-tenant users, return all tenants
            return await _tenantService.GetTenantsAsync(request.Pagination, cancellationToken);
        }
    }
}
