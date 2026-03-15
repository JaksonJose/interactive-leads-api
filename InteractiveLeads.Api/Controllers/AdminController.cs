using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.CrossTenant.Commands;
using InteractiveLeads.Application.Feature.CrossTenant.Queries;
using InteractiveLeads.Application.Feature.Tenancy;
using InteractiveLeads.Application.Feature.Tenancy.Commands;
using InteractiveLeads.Application.Feature.Tenancy.Queries;
using InteractiveLeads.Application.Feature.Users;
using InteractiveLeads.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers
{
    /// <summary>
    /// Admin API for global roles (SysAdmin, Support). Manage tenants and tenant users without being bound to a single tenant.
    /// </summary>
    /// <remarks>
    /// Callable only by users with <b>SysAdmin</b> or <b>Support</b> (global roles; TenantId = null).
    /// Tenant context is not required; the target tenant is specified in the URL (e.g. tenants/{tenantId}).
    /// SysAdmin-only: create/update/activate/deactivate tenants; delete users. SysAdmin and Support: list/get tenants, manage users (create/update/status/roles), get roles.
    /// </remarks>
    [Authorize(Roles = "SysAdmin,Support")]
    public class AdminController : BaseApiController
    {
        // ---- Tenants (SysAdmin only for write) ----

        /// <summary>Create a new tenant. SysAdmin only.</summary>
        [HttpPost("tenants")]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("Create a new tenant (SysAdmin only)")]
        public async Task<IActionResult> CreateTenantAsync([FromBody] CreateTenantRequest request)
        {
            var response = await Sender.Send(new CreateTenantCommand { CreateTenant = request });
            return Ok(response);
        }

        /// <summary>Update an existing tenant. SysAdmin only.</summary>
        [HttpPut("tenants/{tenantId}")]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("Update a tenant (SysAdmin only)")]
        public async Task<IActionResult> UpdateTenantAsync(string tenantId, [FromBody] UpdateTenantRequest request)
        {
            request.Identifier = tenantId;
            var response = await Sender.Send(new UpdateTenantCommand { UpdateTenant = request });
            return Ok(response);
        }

        /// <summary>Activate a tenant. SysAdmin only.</summary>
        [HttpPut("tenants/{tenantId}/activate")]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("Activate a tenant (SysAdmin only)")]
        public async Task<IActionResult> ActivateTenantAsync(string tenantId)
        {
            var response = await Sender.Send(new ActivateTenantCommand { TenantId = tenantId });
            return Ok(response);
        }

        /// <summary>Deactivate a tenant. SysAdmin only.</summary>
        [HttpPut("tenants/{tenantId}/deactivate")]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("Deactivate a tenant (SysAdmin only)")]
        public async Task<IActionResult> DeactivateTenantAsync(string tenantId)
        {
            var response = await Sender.Send(new DeactivateTenantCommand { TenantId = tenantId });
            return Ok(response);
        }

        /// <summary>Get a tenant by id with associated user info. SysAdmin or Support.</summary>
        [HttpGet("tenants/{tenantId}")]
        [OpenApiOperation("Get a tenant by id")]
        public async Task<IActionResult> GetTenantAsync(string tenantId)
        {
            var response = await Sender.Send(new GetTenantWithUserQuery { TenantId = tenantId });
            return Ok(response);
        }

        /// <summary>List tenants accessible to the current user (all for SysAdmin/Support; single for others).</summary>
        [HttpGet("tenants")]
        [OpenApiOperation("List accessible tenants")]
        public async Task<IActionResult> GetTenantsAsync(int pageNumber = 1, int pageSize = 50)
        {
            var pagination = new PaginationRequest { Page = pageNumber, PageSize = pageSize };
            var response = await Sender.Send(new GetAccessibleTenantsQuery { Pagination = pagination });
            return Ok(response);
        }

        // ---- Tenant users (SysAdmin or Support) ----

        /// <summary>List users in a tenant.</summary>
        [HttpGet("tenants/{tenantId}/users")]
        [OpenApiOperation("List users in a tenant")]
        public async Task<IActionResult> GetUsersInTenantAsync(string tenantId)
        {
            var response = await Sender.Send(new GetUsersInTenantQuery { TenantId = tenantId });
            return Ok(response);
        }

        /// <summary>Get a user in a tenant by id.</summary>
        [HttpGet("tenants/{tenantId}/users/{userId}")]
        [OpenApiOperation("Get a user in a tenant")]
        public async Task<IActionResult> GetUserInTenantAsync(string tenantId, Guid userId)
        {
            var response = await Sender.Send(new GetUserInTenantQuery { TenantId = tenantId, UserId = userId });
            return Ok(response);
        }

        /// <summary>Create a user in a tenant.</summary>
        [HttpPost("tenants/{tenantId}/users")]
        [OpenApiOperation("Create a user in a tenant")]
        public async Task<IActionResult> CreateUserInTenantAsync(string tenantId, [FromBody] CreateUserRequest createUser)
        {
            var response = await Sender.Send(new CreateUserInTenantCommand { TenantId = tenantId, CreateUser = createUser });
            return Ok(response);
        }

        /// <summary>Update a user in a tenant.</summary>
        [HttpPut("tenants/{tenantId}/users/{userId}")]
        [OpenApiOperation("Update a user in a tenant")]
        public async Task<IActionResult> UpdateUserInTenantAsync(string tenantId, Guid userId, [FromBody] UpdateUserRequest updateUser)
        {
            var response = await Sender.Send(new UpdateUserInTenantCommand { TenantId = tenantId, UserId = userId, UpdateUser = updateUser });
            return Ok(response);
        }

        /// <summary>Change active status of a user in a tenant.</summary>
        [HttpPut("tenants/{tenantId}/users/{userId}/status")]
        [OpenApiOperation("Change user status in a tenant")]
        public async Task<IActionResult> ChangeUserStatusInTenantAsync(string tenantId, Guid userId, [FromBody] ChangeUserStatusRequest changeUserStatus)
        {
            var response = await Sender.Send(new ChangeUserStatusInTenantCommand { TenantId = tenantId, UserId = userId, ChangeUserStatus = changeUserStatus });
            return Ok(response);
        }

        /// <summary>Update roles of a user in a tenant.</summary>
        [HttpPut("tenants/{tenantId}/users/{userId}/roles")]
        [OpenApiOperation("Update user roles in a tenant")]
        public async Task<IActionResult> UpdateUserRolesInTenantAsync(string tenantId, Guid userId, [FromBody] UserRolesRequest userRolesRequest)
        {
            var response = await Sender.Send(new UpdateUserRolesInTenantCommand { TenantId = tenantId, UserId = userId, UserRolesRequest = userRolesRequest });
            return Ok(response);
        }

        /// <summary>Delete a user from a tenant. SysAdmin only.</summary>
        [HttpDelete("tenants/{tenantId}/users/{userId}")]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("Delete a user from a tenant (SysAdmin only)")]
        public async Task<IActionResult> DeleteUserInTenantAsync(string tenantId, Guid userId)
        {
            var response = await Sender.Send(new DeleteUserInTenantCommand { TenantId = tenantId, UserId = userId });
            return Ok(response);
        }

        /// <summary>Get roles of a user in a tenant.</summary>
        [HttpGet("tenants/{tenantId}/users/{userId}/roles")]
        [OpenApiOperation("Get user roles in a tenant")]
        public async Task<IActionResult> GetUserRolesInTenantAsync(string tenantId, Guid userId)
        {
            var response = await Sender.Send(new GetUserRolesInTenantQuery { TenantId = tenantId, UserId = userId });
            return Ok(response);
        }

        // ---- Tenant roles ----

        /// <summary>List all roles available in a tenant.</summary>
        [HttpGet("tenants/{tenantId}/roles")]
        [OpenApiOperation("Get roles in a tenant")]
        public async Task<IActionResult> GetRolesInTenantAsync(string tenantId)
        {
            var response = await Sender.Send(new GetRolesInTenantQuery { TenantId = tenantId });
            return Ok(response);
        }
    }
}
