using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.Tenancy;
using InteractiveLeads.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Admin
{
    /// <summary>
    /// Admin API: billing plans CRUD (catalog). Limits and features come from DB.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "SysAdmin,Support")]
    public class AdminPlansController : BaseApiController
    {
        private readonly ITenantService _tenantService;

        public AdminPlansController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        /// <summary>List billing plans. Optionally include limits and features.</summary>
        [HttpGet]
        [OpenApiOperation("List billing plans")]
        public async Task<IActionResult> GetPlansAsync([FromQuery] bool includeLimitsAndFeatures = false)
        {
            var response = await _tenantService.GetPlansAsync(includeLimitsAndFeatures);
            return Ok(response);
        }

        /// <summary>Get a single billing plan by id, including limits, features and optionally prices.</summary>
        [HttpGet("{planId:guid}")]
        [OpenApiOperation("Get plan by id")]
        public async Task<IActionResult> GetPlanByIdAsync(Guid planId, [FromQuery] bool includePrices = false)
        {
            var response = await _tenantService.GetPlanByIdAsync(planId, includePrices);
            return Ok(response);
        }

        /// <summary>List plan prices (billing options) for a plan. SysAdmin only.</summary>
        [HttpGet("{planId:guid}/prices")]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("List plan prices (SysAdmin only)")]
        public async Task<IActionResult> GetPlanPricesAsync(Guid planId)
        {
            var response = await _tenantService.GetPlanPricesAsync(planId);
            return Ok(response);
        }

        /// <summary>Create a plan price (billing option). SysAdmin only.</summary>
        [HttpPost("{planId:guid}/prices")]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("Create plan price (SysAdmin only)")]
        public async Task<IActionResult> CreatePlanPriceAsync(Guid planId, [FromBody] CreatePlanPriceRequest request)
        {
            var response = await _tenantService.CreatePlanPriceAsync(planId, request);
            return Ok(response);
        }

        /// <summary>Update a plan price. SysAdmin only.</summary>
        [HttpPut("{planId:guid}/prices/{priceId:guid}")]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("Update plan price (SysAdmin only)")]
        public async Task<IActionResult> UpdatePlanPriceAsync(Guid planId, Guid priceId, [FromBody] UpdatePlanPriceRequest request)
        {
            var response = await _tenantService.UpdatePlanPriceAsync(planId, priceId, request);
            return Ok(response);
        }

        /// <summary>Create a new billing plan with optional limits and features. SysAdmin only.</summary>
        [HttpPost]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("Create a billing plan (SysAdmin only)")]
        public async Task<IActionResult> CreatePlanAsync([FromBody] CreatePlanRequest request)
        {
            var response = await _tenantService.CreatePlanAsync(request);
            return Ok(response);
        }

        /// <summary>Update an existing billing plan. When Limits/Features are sent, they replace existing ones. SysAdmin only.</summary>
        [HttpPut("{planId:guid}")]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("Update a billing plan (SysAdmin only)")]
        public async Task<IActionResult> UpdatePlanAsync(Guid planId, [FromBody] UpdatePlanRequest request)
        {
            var response = await _tenantService.UpdatePlanAsync(planId, request);
            return Ok(response);
        }
    }
}
