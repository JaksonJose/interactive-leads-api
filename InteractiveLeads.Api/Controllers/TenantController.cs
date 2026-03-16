using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InteractiveLeads.Api.Controllers
{
    /// <summary>
    /// Endpoints for the current tenant context (subscription, features). Tenant is resolved from header/JWT.
    /// </summary>
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TenantController : BaseApiController
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ISubscriptionPlanService _subscriptionPlanService;

        public TenantController(ICurrentUserService currentUserService, ISubscriptionPlanService subscriptionPlanService)
        {
            _currentUserService = currentUserService;
            _subscriptionPlanService = subscriptionPlanService;
        }

        /// <summary>
        /// Gets the set of feature keys enabled for the current tenant's plan. Used by the frontend to show/hide features.
        /// </summary>
        [HttpGet("features")]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
        public async Task<ActionResult<string[]>> GetEnabledFeatures(CancellationToken cancellationToken)
        {
            var tenantId = _currentUserService.GetUserTenant();
            if (string.IsNullOrEmpty(tenantId))
                return Ok(Array.Empty<string>());

            var features = await _subscriptionPlanService.GetEnabledFeaturesAsync(tenantId, cancellationToken);
            return Ok(features.ToArray());
        }
    }
}
