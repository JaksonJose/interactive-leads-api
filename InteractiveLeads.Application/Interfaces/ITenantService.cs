using InteractiveLeads.Application.Feature.Tenancy;
using InteractiveLeads.Application.Models;
using InteractiveLeads.Application.Responses;

namespace InteractiveLeads.Application.Interfaces
{
    /// <summary>
    /// Service interface for managing tenant operations in the multi-tenant system.
    /// </summary>
    /// <remarks>
    /// Provides methods for creating, activating, deactivating, and managing tenant subscriptions.
    /// </remarks>
    public interface ITenantService
    {
        /// <summary>
        /// Creates a new tenant with the provided information.
        /// </summary>
        /// <param name="createTenantRequest">The request containing tenant details.</param>
        /// <param name="ct">Cancellation token for the async operation.</param>
        /// <returns>Result of the tenant creation operation.</returns>
        Task<ResultResponse> CreateTenantAsync(CreateTenantRequest createTenantRequest, CancellationToken ct);

        /// <summary>
        /// Activates a tenant, enabling access to the system.
        /// </summary>
        /// <param name="id">The unique identifier of the tenant to activate.</param>
        /// <param name="ct">Cancellation token for the async operation.</param>
        /// <returns>Result of the tenant activation operation.</returns>
        Task<ResultResponse> ActivateAsync(string id, CancellationToken ct = default);

        /// <summary>
        /// Deactivates a tenant, disabling access to the system.
        /// </summary>
        /// <param name="id">The unique identifier of the tenant to deactivate.</param>
        /// <param name="ct">Cancellation token for the async operation.</param>
        /// <returns>Result of the tenant deactivation operation.</returns>
        Task<ResultResponse> DeactivateAsync(string id, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing tenant with the provided information.
        /// </summary>
        /// <param name="updateTenantRequest">The request containing updated tenant details.</param>
        /// <param name="ct">Cancellation token for the async operation.</param>
        /// <returns>Result of the tenant update operation.</returns>
        Task<ResultResponse> UpdateTenantAsync(UpdateTenantRequest updateTenantRequest, CancellationToken ct = default);

        /// <summary>
        /// Updates the subscription information for a tenant.
        /// </summary>
        /// <param name="updateTenantSubscriptionRequest">The request containing updated subscription details.</param>
        /// <param name="ct">Cancellation token for the async operation.</param>
        /// <returns>Result of the subscription update operation.</returns>
        Task<ResultResponse> UpdateSubscriptionAsync(UpdateTenantSubscriptionRequest updateTenantSubscriptionRequest, CancellationToken ct = default);

        /// <summary>
        /// Retrieves tenants in the system with pagination support.
        /// </summary>
        /// <param name="pagination">Pagination parameters for the query.</param>
        /// <param name="ct">Cancellation token for the async operation.</param>
        /// <returns>Paginated list of tenants.</returns>
        Task<ListResponse<TenantResponse>> GetTenantsAsync(PaginationRequest pagination, CancellationToken ct);

        /// <summary>
        /// Retrieves a specific tenant by its identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the tenant to retrieve.</param>
        /// <param name="ct">Cancellation token for the async operation.</param>
        /// <returns>Tenant data if found.</returns>
        Task<SingleResponse<TenantResponse>> GetTenantsByIdAsync(string id, CancellationToken ct);

        /// <summary>
        /// Lists all billing plans (from DB). Optionally include limits and features.
        /// </summary>
        Task<ListResponse<PlanResponse>> GetPlansAsync(bool includeLimitsAndFeatures, CancellationToken ct = default);

        /// <summary>
        /// Gets a single billing plan by id, including limits, features and prices when requested.
        /// </summary>
        Task<SingleResponse<PlanResponse>> GetPlanByIdAsync(Guid planId, bool includePrices = false, CancellationToken ct = default);

        /// <summary>
        /// Lists plan prices (billing options) for a plan.
        /// </summary>
        Task<ListResponse<PlanPriceResponse>> GetPlanPricesAsync(Guid planId, CancellationToken ct = default);

        /// <summary>
        /// Creates a plan price (billing option) for a plan.
        /// </summary>
        Task<SingleResponse<PlanPriceResponse>> CreatePlanPriceAsync(Guid planId, CreatePlanPriceRequest request, CancellationToken ct = default);

        /// <summary>
        /// Updates a plan price.
        /// </summary>
        Task<SingleResponse<PlanPriceResponse>> UpdatePlanPriceAsync(Guid planId, Guid priceId, UpdatePlanPriceRequest request, CancellationToken ct = default);

        /// <summary>
        /// Creates a new billing plan with optional limits and features.
        /// </summary>
        Task<SingleResponse<PlanResponse>> CreatePlanAsync(CreatePlanRequest request, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing billing plan. When Limits/Features are provided, they replace existing ones.
        /// </summary>
        Task<SingleResponse<PlanResponse>> UpdatePlanAsync(Guid planId, UpdatePlanRequest request, CancellationToken ct = default);

        /// <summary>
        /// Assigns or updates the tenant's subscription to the given plan and end date. Ensures at most one active subscription per tenant.
        /// </summary>
        Task<ResultResponse> AssignSubscriptionAsync(AssignTenantSubscriptionRequest request, CancellationToken ct = default);
    }
}
