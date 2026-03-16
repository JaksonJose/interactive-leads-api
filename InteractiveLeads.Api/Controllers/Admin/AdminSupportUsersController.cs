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
    /// Admin API: global support users (SysAdmin and Support; TenantId = null).
    /// </summary>
    [Authorize(Roles = "SysAdmin,Support")]
    public class AdminSupportUsersController : BaseApiController
    {
        /// <summary>List all global users (SysAdmin and Support).</summary>
        [HttpGet]
        [OpenApiOperation("List global support users")]
        public async Task<IActionResult> GetGlobalUsersAsync()
        {
            var response = await Sender.Send(new GetGlobalUsersQuery());
            return Ok(response);
        }

        /// <summary>Create a new Support user (global user). SysAdmin only.</summary>
        [HttpPost]
        [Authorize(Roles = "SysAdmin")]
        [OpenApiOperation("Create a support user (SysAdmin only)")]
        public async Task<IActionResult> CreateSupportUserAsync([FromBody] CreateUserRequest createUser)
        {
            var response = await Sender.Send(new CreateSupportUserCommand { CreateUser = createUser });
            return Ok(response);
        }
    }
}
