using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.CrossTenant.Queries;
using InteractiveLeads.Application.Feature.Tenancy;
using InteractiveLeads.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Owner
{
    /// <summary>
    /// Owner API: current tenant data, subscription (upgrade/downgrade), and plans catalog. Tenant is resolved from the current user context.
    /// </summary>
    [Authorize(Roles = "Owner")]
    public class OwnerTenantController : BaseApiController
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITenantService _tenantService;

        public OwnerTenantController(ICurrentUserService currentUserService, ITenantService tenantService)
        {
            _currentUserService = currentUserService;
            _tenantService = tenantService;
        }

        /// <summary>Get the current tenant (owner's tenant) with associated user info.</summary>
        [HttpGet]
        [OpenApiOperation("Get current tenant")]
        public async Task<IActionResult> GetCurrentTenantAsync()
        {
            var tenantId = _currentUserService.GetUserTenant();
            if (string.IsNullOrEmpty(tenantId))
                return BadRequest(new { message = "Tenant context is required." });

            var response = await Sender.Send(new GetTenantWithUserQuery { TenantId = tenantId });
            return Ok(response);
        }

        /// <summary>Assign or change the current tenant's subscription (upgrade/downgrade).</summary>
        [HttpPut("subscription")]
        [OpenApiOperation("Assign tenant subscription")]
        public async Task<IActionResult> AssignSubscriptionAsync([FromBody] OwnerAssignSubscriptionRequest request)
        {
            var tenantId = _currentUserService.GetUserTenant();
            if (string.IsNullOrEmpty(tenantId))
                return BadRequest(new { message = "Tenant context is required." });

            var assignRequest = new AssignTenantSubscriptionRequest
            {
                TenantId = tenantId,
                PlanPriceId = request.PlanPriceId,
                EndDate = request.EndDate
            };
            var response = await _tenantService.AssignSubscriptionAsync(assignRequest);
            return Ok(response);
        }

        /// <summary>List billing plans (for upgrade/downgrade). Optionally include limits and features.</summary>
        [HttpGet("plans")]
        [OpenApiOperation("List billing plans")]
        public async Task<IActionResult> GetPlansAsync([FromQuery] bool includeLimitsAndFeatures = false)
        {
            var response = await _tenantService.GetPlansAsync(includeLimitsAndFeatures);
            return Ok(response);
        }

        /// <summary>Get a single billing plan by id, including prices.</summary>
        [HttpGet("plans/{planId:guid}")]
        [OpenApiOperation("Get plan by id")]
        public async Task<IActionResult> GetPlanByIdAsync(Guid planId, [FromQuery] bool includePrices = true)
        {
            var response = await _tenantService.GetPlanByIdAsync(planId, includePrices);
            return Ok(response);
        }

        /// <summary>List plan prices (billing options) for a plan.</summary>
        [HttpGet("plans/{planId:guid}/prices")]
        [OpenApiOperation("List plan prices")]
        public async Task<IActionResult> GetPlanPricesAsync(Guid planId)
        {
            var response = await _tenantService.GetPlanPricesAsync(planId);
            return Ok(response);
        }
    }
}
