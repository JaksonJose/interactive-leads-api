using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.Identity.Roles;
using InteractiveLeads.Application.Feature.Identity.Roles.Commands;
using InteractiveLeads.Application.Feature.Identity.Roles.Queries;
using Microsoft.AspNetCore.Mvc;

namespace InteractiveLeads.Api.Controllers
{
    /// <summary>
    /// Controller for role management operations.
    /// </summary>
    /// <remarks>
    /// Provides endpoints for creating, updating, deleting and retrieving system roles.
    /// </remarks>
    public class RolesController : BaseApiController
    {
        /// <summary>
        /// Creates a new role in the system.
        /// </summary>
        /// <param name="createRole">Role data to be created.</param>
        /// <returns>Result of the role creation operation.</returns>
        [HttpPost("add")]
        public async Task<IActionResult> AddRoleAsync([FromBody] CreateRoleRequest createRole)
        {
            var response = await Sender.Send(new CreateRoleCommand { CreateRole = createRole });

            return Ok(response);
        }

        /// <summary>
        /// Updates an existing role.
        /// </summary>
        /// <param name="updateRole">Updated role data.</param>
        /// <returns>Result of the role update operation.</returns>
        [HttpPut("update")]
        public async Task<IActionResult> UpdateRoleAsync([FromBody] UpdateRoleRequest updateRole)
        {
            var response = await Sender.Send(new UpdateRoleCommand { UpdateRole = updateRole });

            return Ok(response);
        }

        /// <summary>
        /// Removes a role from the system.
        /// </summary>
        /// <param name="roleId">Unique identifier of the role to be removed.</param>
        /// <returns>Result of the role removal operation.</returns>
        [HttpDelete("delete/{roleId}")]
        public async Task<IActionResult> DeleteRoleAsync(Guid roleId)
        {
            var response = await Sender.Send(new DeleteRoleCommand { RoleId = roleId });

            return Ok(response);
        }

        /// <summary>
        /// Retrieves all roles in the system.
        /// </summary>
        /// <returns>List of all roles.</returns>
        [HttpGet("all")]
        public async Task<IActionResult> GetRolesAsync()
        {
            var response = await Sender.Send(new GetRolesQuery());

            return Ok(response);
        }

        /// <summary>
        /// Retrieves a role by its identifier without permissions.
        /// </summary>
        /// <param name="roleId">Unique identifier of the role.</param>
        /// <returns>Role data without permissions.</returns>
        [HttpGet("partial/{roleId}")]
        public async Task<IActionResult> GetPartialRoleByIdAsync(Guid roleId)
        {
            var response = await Sender.Send(new GetRoleByIdQuery { RoleId = roleId });

            return Ok(response);
        }

        /// <summary>
        /// Retrieves a role by its identifier including permissions.
        /// </summary>
        /// <param name="roleId">Unique identifier of the role.</param>
        /// <returns>Role data with permissions.</returns>
        [HttpGet("full/{roleId}")]
        public async Task<IActionResult> GetDetailedRoleByIdAsync(Guid roleId)
        {
            var response = await Sender.Send(new GetRoleWithPermissionsQuery { RoleId = roleId });

            return Ok(response);
        }
    }
}
