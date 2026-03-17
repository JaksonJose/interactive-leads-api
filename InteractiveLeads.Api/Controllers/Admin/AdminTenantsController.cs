using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.CrossTenant.Queries;
using InteractiveLeads.Application.Feature.Tenancy;
using InteractiveLeads.Application.Feature.Tenancy.Commands;
using InteractiveLeads.Application.Feature.Tenancy.Queries;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Admin
{
    /// <summary>
    /// Admin API: tenant CRUD, activate/deactivate, and subscription assignment.
    /// </summary>
    [Authorize(Roles = "SysAdmin,Support")]
    public class AdminTenantsController : BaseApiController
    {
        private readonly ITenantService _tenantService;

        public AdminTenantsController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        /// <summary>List tenants accessible to the current user. Accepts pagination and filters in body (POST for complex query).</summary>
        [HttpPost("list")]
        [OpenApiOperation("List accessible tenants")]
        public async Task<IActionResult> GetTenantsAsync([FromBody] InquiryRequest request)
        {
            var response = await Sender.Send(new GetAccessibleTenantsQuery { Pagination = request ?? new InquiryRequest() });
            return Ok(response);
        }

        /// <summary>Create a new tenant. SysAdmin only.</summary>
        [HttpPost]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("Create a new tenant (SysAdmin only)")]
        public async Task<IActionResult> CreateTenantAsync([FromBody] CreateTenantRequest request)
        {
            var response = await Sender.Send(new CreateTenantCommand { CreateTenant = request });
            return Ok(response);
        }

        /// <summary>Get a tenant by id with associated user info.</summary>
        [HttpGet("{tenantId}")]
        [OpenApiOperation("Get a tenant by id")]
        public async Task<IActionResult> GetTenantAsync(string tenantId)
        {
            var response = await Sender.Send(new GetTenantWithUserQuery { TenantId = tenantId });
            return Ok(response);
        }

        /// <summary>Update an existing tenant. SysAdmin only.</summary>
        [HttpPut("{tenantId}")]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("Update a tenant (SysAdmin only)")]
        public async Task<IActionResult> UpdateTenantAsync(string tenantId, [FromBody] UpdateTenantRequest request)
        {
            request.Identifier = tenantId;
            var response = await Sender.Send(new UpdateTenantCommand { UpdateTenant = request });
            return Ok(response);
        }

        /// <summary>Activate a tenant. SysAdmin only.</summary>
        [HttpPut("{tenantId}/activate")]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("Activate a tenant (SysAdmin only)")]
        public async Task<IActionResult> ActivateTenantAsync(string tenantId)
        {
            var response = await Sender.Send(new ActivateTenantCommand { TenantId = tenantId });
            return Ok(response);
        }

        /// <summary>Deactivate a tenant. SysAdmin only.</summary>
        [HttpPut("{tenantId}/deactivate")]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("Deactivate a tenant (SysAdmin only)")]
        public async Task<IActionResult> DeactivateTenantAsync(string tenantId)
        {
            var response = await Sender.Send(new DeactivateTenantCommand { TenantId = tenantId });
            return Ok(response);
        }

        /// <summary>Assign or change a tenant's subscription to a plan and end date. SysAdmin only.</summary>
        [HttpPut("{tenantId}/subscription")]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("Assign tenant subscription (SysAdmin only)")]
        public async Task<IActionResult> AssignTenantSubscriptionAsync(string tenantId, [FromBody] AssignTenantSubscriptionRequest request)
        {
            request.TenantId = tenantId;
            var response = await _tenantService.AssignSubscriptionAsync(request);
            return Ok(response);
        }

        /// <summary>Resend activation invitation for tenant owner. SysAdmin only.</summary>
        [HttpPost("{tenantId}/owner/{ownerUserId:guid}/resend-activation")]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("Resend activation invitation for tenant owner (SysAdmin only)")]
        public async Task<IActionResult> ResendTenantOwnerActivationAsync(string tenantId, Guid ownerUserId)
        {
            var response = await Sender.Send(new ResendTenantOwnerActivationCommand
            {
                TenantId = tenantId,
                OwnerUserId = ownerUserId
            });
            return Ok(response);
        }
    }
}
