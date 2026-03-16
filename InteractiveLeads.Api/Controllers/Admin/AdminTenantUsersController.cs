using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.CrossTenant.Commands;
using InteractiveLeads.Application.Feature.CrossTenant.Queries;
using InteractiveLeads.Application.Feature.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Admin
{
    /// <summary>
    /// Admin API: users and roles within a specific tenant.
    /// </summary>
    [Authorize(Roles = "SysAdmin,Support")]
    public class AdminTenantUsersController : BaseApiController
    {
        /// <summary>List all roles available in the tenant.</summary>
        [HttpGet("roles")]
        [OpenApiOperation("Get roles in a tenant")]
        public async Task<IActionResult> GetRolesInTenantAsync(string tenantId)
        {
            var response = await Sender.Send(new GetRolesInTenantQuery { TenantId = tenantId });
            return Ok(response);
        }

        /// <summary>List users in the tenant.</summary>
        [HttpGet("users")]
        [OpenApiOperation("List users in a tenant")]
        public async Task<IActionResult> GetUsersInTenantAsync(string tenantId)
        {
            var response = await Sender.Send(new GetUsersInTenantQuery { TenantId = tenantId });
            return Ok(response);
        }

        /// <summary>Get a user in the tenant by id.</summary>
        [HttpGet("users/{userId:guid}")]
        [OpenApiOperation("Get a user in a tenant")]
        public async Task<IActionResult> GetUserInTenantAsync(string tenantId, Guid userId)
        {
            var response = await Sender.Send(new GetUserInTenantQuery { TenantId = tenantId, UserId = userId });
            return Ok(response);
        }

        /// <summary>Create a user in the tenant.</summary>
        [HttpPost("users")]
        [OpenApiOperation("Create a user in a tenant")]
        public async Task<IActionResult> CreateUserInTenantAsync(string tenantId, [FromBody] CreateUserRequest createUser)
        {
            var response = await Sender.Send(new CreateUserInTenantCommand { TenantId = tenantId, CreateUser = createUser });
            return Ok(response);
        }

        /// <summary>Update a user in the tenant.</summary>
        [HttpPut("users/{userId:guid}")]
        [OpenApiOperation("Update a user in a tenant")]
        public async Task<IActionResult> UpdateUserInTenantAsync(string tenantId, Guid userId, [FromBody] UpdateUserRequest updateUser)
        {
            var response = await Sender.Send(new UpdateUserInTenantCommand { TenantId = tenantId, UserId = userId, UpdateUser = updateUser });
            return Ok(response);
        }

        /// <summary>Change active status of a user in the tenant.</summary>
        [HttpPut("users/{userId:guid}/status")]
        [OpenApiOperation("Change user status in a tenant")]
        public async Task<IActionResult> ChangeUserStatusInTenantAsync(string tenantId, Guid userId, [FromBody] ChangeUserStatusRequest changeUserStatus)
        {
            var response = await Sender.Send(new ChangeUserStatusInTenantCommand { TenantId = tenantId, UserId = userId, ChangeUserStatus = changeUserStatus });
            return Ok(response);
        }

        /// <summary>Update roles of a user in the tenant.</summary>
        [HttpPut("users/{userId:guid}/roles")]
        [OpenApiOperation("Update user roles in a tenant")]
        public async Task<IActionResult> UpdateUserRolesInTenantAsync(string tenantId, Guid userId, [FromBody] UserRolesRequest userRolesRequest)
        {
            var response = await Sender.Send(new UpdateUserRolesInTenantCommand { TenantId = tenantId, UserId = userId, UserRolesRequest = userRolesRequest });
            return Ok(response);
        }

        /// <summary>Get roles of a user in the tenant.</summary>
        [HttpGet("users/{userId:guid}/roles")]
        [OpenApiOperation("Get user roles in a tenant")]
        public async Task<IActionResult> GetUserRolesInTenantAsync(string tenantId, Guid userId)
        {
            var response = await Sender.Send(new GetUserRolesInTenantQuery { TenantId = tenantId, UserId = userId });
            return Ok(response);
        }

        /// <summary>Delete a user from the tenant. SysAdmin only.</summary>
        [HttpDelete("users/{userId:guid}")]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("Delete a user from a tenant (SysAdmin only)")]
        public async Task<IActionResult> DeleteUserInTenantAsync(string tenantId, Guid userId)
        {
            var response = await Sender.Send(new DeleteUserInTenantCommand { TenantId = tenantId, UserId = userId });
            return Ok(response);
        }
    }
}
