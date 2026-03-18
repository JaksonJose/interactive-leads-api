using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.CrossTenant.Commands;
using InteractiveLeads.Application.Feature.CrossTenant.Queries;
using InteractiveLeads.Application.Feature.Owner.Commands;
using InteractiveLeads.Application.Feature.Users;
using InteractiveLeads.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Owner
{
    /// <summary>
    /// Owner/Manager API: users and roles within the tenant. Tenant is resolved from the current user context.
    /// Owner and Manager can list, add, edit and manage consultants.
    /// </summary>
    [Authorize(Roles = "Owner,Manager")]
    public class OwnerConsultantsController : BaseApiController
    {
        private readonly ICurrentUserService _currentUserService;

        public OwnerConsultantsController(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        private IActionResult? EnsureTenantId(out string tenantId)
        {
            tenantId = _currentUserService.GetUserTenant() ?? string.Empty;
            if (string.IsNullOrEmpty(tenantId))
                return BadRequest(new { message = "Tenant context is required." });
            return null;
        }

        /// <summary>List all roles available in the tenant.</summary>
        [HttpGet("roles")]
        [OpenApiOperation("Get roles in tenant")]
        public async Task<IActionResult> GetRolesInTenantAsync()
        {
            if (EnsureTenantId(out var tenantId) is { } err) return err;
            var response = await Sender.Send(new GetRolesInTenantQuery { TenantId = tenantId });
            return Ok(response);
        }

        /// <summary>List users in the tenant (consultants).</summary>
        [HttpGet("users")]
        [OpenApiOperation("List users in tenant")]
        public async Task<IActionResult> GetUsersInTenantAsync()
        {
            if (EnsureTenantId(out var tenantId) is { } err) return err;
            var response = await Sender.Send(new GetUsersInTenantQuery { TenantId = tenantId });
            return Ok(response);
        }

        /// <summary>Get a user in the tenant by id.</summary>
        [HttpGet("users/{userId:guid}")]
        [OpenApiOperation("Get user in tenant")]
        public async Task<IActionResult> GetUserInTenantAsync(Guid userId)
        {
            if (EnsureTenantId(out var tenantId) is { } err) return err;
            var response = await Sender.Send(new GetUserInTenantQuery { TenantId = tenantId, UserId = userId });
            return Ok(response);
        }

        /// <summary>Create a user in the tenant.</summary>
        [HttpPost("users")]
        [OpenApiOperation("Create user in tenant")]
        public async Task<IActionResult> CreateUserInTenantAsync([FromBody] CreateUserRequest createUser)
        {
            if (EnsureTenantId(out var tenantId) is { } err) return err;
            var response = await Sender.Send(new CreateUserInTenantCommand { TenantId = tenantId, CreateUser = createUser });
            return Ok(response);
        }

        /// <summary>Invite a consultant in the tenant (creates user without password and returns activation URL).</summary>
        [HttpPost("consultants/invite")]
        [OpenApiOperation("Invite consultant in tenant")]
        public async Task<IActionResult> InviteConsultantAsync([FromBody] InviteUserRequest inviteUser)
        {
            if (EnsureTenantId(out var tenantId) is { } err) return err;
            var response = await Sender.Send(new InviteConsultantCommand { TenantId = tenantId, InviteUser = inviteUser });
            return Ok(response);
        }

        /// <summary>Update a user in the tenant.</summary>
        [HttpPut("users/{userId:guid}")]
        [OpenApiOperation("Update user in tenant")]
        public async Task<IActionResult> UpdateUserInTenantAsync(Guid userId, [FromBody] UpdateUserRequest updateUser)
        {
            if (EnsureTenantId(out var tenantId) is { } err) return err;
            var response = await Sender.Send(new UpdateUserInTenantCommand { TenantId = tenantId, UserId = userId, UpdateUser = updateUser });
            return Ok(response);
        }

        /// <summary>Change active status of a user in the tenant (activate/deactivate).</summary>
        [HttpPut("users/{userId:guid}/status")]
        [OpenApiOperation("Change user status in tenant")]
        public async Task<IActionResult> ChangeUserStatusInTenantAsync(Guid userId, [FromBody] ChangeUserStatusRequest changeUserStatus)
        {
            if (EnsureTenantId(out var tenantId) is { } err) return err;
            var response = await Sender.Send(new ChangeUserStatusInTenantCommand { TenantId = tenantId, UserId = userId, ChangeUserStatus = changeUserStatus });
            return Ok(response);
        }

        /// <summary>Update roles of a user in the tenant.</summary>
        [HttpPut("users/{userId:guid}/roles")]
        [OpenApiOperation("Update user roles in tenant")]
        public async Task<IActionResult> UpdateUserRolesInTenantAsync(Guid userId, [FromBody] UserRolesRequest userRolesRequest)
        {
            if (EnsureTenantId(out var tenantId) is { } err) return err;
            var response = await Sender.Send(new UpdateUserRolesInTenantCommand { TenantId = tenantId, UserId = userId, UserRolesRequest = userRolesRequest });
            return Ok(response);
        }

        /// <summary>Get roles of a user in the tenant.</summary>
        [HttpGet("users/{userId:guid}/roles")]
        [OpenApiOperation("Get user roles in tenant")]
        public async Task<IActionResult> GetUserRolesInTenantAsync(Guid userId)
        {
            if (EnsureTenantId(out var tenantId) is { } err) return err;
            var response = await Sender.Send(new GetUserRolesInTenantQuery { TenantId = tenantId, UserId = userId });
            return Ok(response);
        }

        /// <summary>Resend activation invitation for a consultant in the tenant.</summary>
        [HttpPost("consultants/{userId:guid}/resend-activation")]
        [OpenApiOperation("Resend activation invitation for consultant in tenant")]
        public async Task<IActionResult> ResendConsultantInvitationAsync(Guid userId)
        {
            if (EnsureTenantId(out var tenantId) is { } err) return err;
            var response = await Sender.Send(new ResendConsultantInvitationCommand { TenantId = tenantId, UserId = userId });
            return Ok(response);
        }
    }
}
