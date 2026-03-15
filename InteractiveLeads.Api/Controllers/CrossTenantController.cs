using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.CrossTenant.Commands;
using InteractiveLeads.Application.Feature.CrossTenant.Queries;
using InteractiveLeads.Application.Feature.Tenancy;
using InteractiveLeads.Application.Feature.Tenancy.Commands;
using InteractiveLeads.Application.Feature.Tenancy.Queries;
using InteractiveLeads.Application.Feature.Users;
using InteractiveLeads.Application.Models;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers
{
    /// <summary>
    /// Controller for cross-tenant operations allowing access to multiple tenants.
    /// </summary>
    /// <remarks>
    /// Provides endpoints for SysAdmin and Support users to manage users across different tenants.
    /// Authorization is by role only (SysAdmin or Support as documented per endpoint).
    /// </remarks>
    public class CrossTenantController : BaseApiController
    {
        /// <summary>
        /// Initializes a new instance of the CrossTenantController class.
        /// </summary>
        public CrossTenantController()
        {
        }

        /// <summary>
        /// Creates a new tenant in the system - SysAdmin only.
        /// </summary>
        /// <param name="request">The tenant creation request containing tenant details.</param>
        /// <returns>Result of the tenant creation operation.</returns>
        /// <remarks>
        /// Requires role SysAdmin.
        /// </remarks>
        [HttpPost("add")]
        [OpenApiOperation("Create a new tenant")]
        public async Task<IActionResult> CreateTenantAsync([FromBody] CreateTenantRequest request)
        {
            var response = await Sender.Send(new CreateTenantCommand { CreateTenant = request });
            return Ok(response);
        }

        /// <summary>
        /// Updates an existing tenant in the system - SysAdmin only.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant to update.</param>
        /// <param name="request">The tenant update request containing updated tenant details.</param>
        /// <returns>Result of the tenant update operation.</returns>
        /// <remarks>
        /// Requires role SysAdmin.
        /// </remarks>
        [HttpPut("{tenantId}")]
        [OpenApiOperation("Update an existing tenant")]
        public async Task<IActionResult> UpdateTenantAsync(string tenantId, [FromBody] UpdateTenantRequest request)
        {
            // Ensure the identifier in the request matches the tenantId in the route
            request.Identifier = tenantId;
            var response = await Sender.Send(new UpdateTenantCommand { UpdateTenant = request });
            return Ok(response);
        }

        /// <summary>
        /// Activates an existing tenant in the system - SysAdmin only.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant to activate.</param>
        /// <returns>Result of the tenant activation operation.</returns>
        /// <remarks>
        /// Requires role SysAdmin.
        /// </remarks>
        [HttpPut("{tenantId}/activate")]
        [OpenApiOperation("Activate an existing tenant")]
        public async Task<IActionResult> ActivateTenantAsync(string tenantId)
        {
            var response = await Sender.Send(new ActivateTenantCommand { TenantId = tenantId });
            return Ok(response);
        }

        /// <summary>
        /// Deactivates an existing tenant in the system - SysAdmin only.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant to deactivate.</param>
        /// <returns>Result of the tenant deactivation operation.</returns>
        /// <remarks>
        /// Requires role SysAdmin.
        /// </remarks>
        [HttpPut("{tenantId}/deactivate")]
        [OpenApiOperation("Deactivate an existing tenant")]
        public async Task<IActionResult> DeactivateTenantAsync(string tenantId)
        {
            var response = await Sender.Send(new DeactivateTenantCommand { TenantId = tenantId });
            return Ok(response);
        }

        /// <summary>
        /// Lists users in a specific tenant - available for SysAdmin and Support.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant to list users from.</param>
        /// <returns>List of users in the specified tenant.</returns>
        /// <remarks>
        /// Requires role SysAdmin or Support.
        /// </remarks>
        [HttpGet("tenants/{tenantId}/users")]
        [OpenApiOperation("List users in a specific tenant")]
        public async Task<IActionResult> GetUsersInTenantAsync(string tenantId)
        {
            var response = await Sender.Send(new GetUsersInTenantQuery { TenantId = tenantId });
            
            return Ok(response);
        }

        /// <summary>
        /// Gets a specific user from a tenant - available for SysAdmin and Support.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <param name="userId">The ID of the user to retrieve.</param>
        /// <returns>User data from the specified tenant.</returns>
        /// <remarks>
        /// Requires role SysAdmin or Support.
        /// </remarks>
        [HttpGet("tenants/{tenantId}/users/{userId}")]
        [OpenApiOperation("Get a specific user from a tenant")]
        public async Task<IActionResult> GetUserInTenantAsync(string tenantId, Guid userId)
        {
            var response = await Sender.Send(new GetUserInTenantQuery { TenantId = tenantId, UserId = userId });
            
            return Ok(response);
        }

        /// <summary>
        /// Creates a new user in a specific tenant - available for SysAdmin and Support.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant to create the user in.</param>
        /// <param name="createUser">User data to be created.</param>
        /// <returns>Result of the user creation operation.</returns>
        /// <remarks>
        /// Requires role SysAdmin or Support.
        /// SysAdmin and Support users can create users in other tenants.
        /// </remarks>
        [HttpPost("tenants/{tenantId}/users")]
        [OpenApiOperation("Create a user in a specific tenant")]
        public async Task<IActionResult> CreateUserInTenantAsync(string tenantId, [FromBody] CreateUserRequest createUser)
        {
            var response = await Sender.Send(new CreateUserInTenantCommand 
            { 
                TenantId = tenantId, 
                CreateUser = createUser 
            });
            
            return Ok(response);
        }

        /// <summary>
        /// Updates a user in a specific tenant - available for SysAdmin and Support.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <param name="userId">The ID of the user to update.</param>
        /// <param name="updateUser">Updated user data.</param>
        /// <returns>Result of the user update operation.</returns>
        /// <remarks>
        /// Requires role SysAdmin or Support.
        /// </remarks>
        [HttpPut("tenants/{tenantId}/users/{userId}")]
        [OpenApiOperation("Update a user in a specific tenant")]
        public async Task<IActionResult> UpdateUserInTenantAsync(string tenantId, Guid userId, [FromBody] UpdateUserRequest updateUser)
        {
            var response = await Sender.Send(new UpdateUserInTenantCommand 
            { 
                TenantId = tenantId, 
                UserId = userId, 
                UpdateUser = updateUser 
            });
            
            return Ok(response);
        }

        /// <summary>
        /// Changes user status in a specific tenant - available for SysAdmin and Support.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <param name="userId">The ID of the user to update.</param>
        /// <param name="changeUserStatus">User status change data.</param>
        /// <returns>Result of the status change operation.</returns>
        /// <remarks>
        /// Requires role SysAdmin or Support.
        /// </remarks>
        [HttpPut("tenants/{tenantId}/users/{userId}/status")]
        [OpenApiOperation("Change user status in a specific tenant")]
        public async Task<IActionResult> ChangeUserStatusInTenantAsync(string tenantId, Guid userId, [FromBody] ChangeUserStatusRequest changeUserStatus)
        {
            var response = await Sender.Send(new ChangeUserStatusInTenantCommand 
            { 
                TenantId = tenantId, 
                UserId = userId, 
                ChangeUserStatus = changeUserStatus 
            });
            
            return Ok(response);
        }

        /// <summary>
        /// Updates user roles in a specific tenant - available for SysAdmin and Support.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <param name="userId">The ID of the user to update.</param>
        /// <param name="userRolesRequest">List of roles to be assigned to the user.</param>
        /// <returns>Result of the roles update operation.</returns>
        /// <remarks>
        /// Requires role SysAdmin or Support.
        /// </remarks>
        [HttpPut("tenants/{tenantId}/users/{userId}/roles")]
        [OpenApiOperation("Update user roles in a specific tenant")]
        public async Task<IActionResult> UpdateUserRolesInTenantAsync(string tenantId, Guid userId, [FromBody] UserRolesRequest userRolesRequest)
        {
            var response = await Sender.Send(new UpdateUserRolesInTenantCommand 
            { 
                TenantId = tenantId, 
                UserId = userId, 
                UserRolesRequest = userRolesRequest 
            });
            
            return Ok(response);
        }

        /// <summary>
        /// Deletes a user from a specific tenant - SysAdmin only.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <param name="userId">The ID of the user to delete.</param>
        /// <returns>Result of the user deletion operation.</returns>
        /// <remarks>
        /// Requires role SysAdmin.
        /// Only SysAdmin users can delete users from other tenants.
        /// </remarks>
        [HttpDelete("tenants/{tenantId}/users/{userId}")]
        [OpenApiOperation("Delete a user from a specific tenant")]
        public async Task<IActionResult> DeleteUserInTenantAsync(string tenantId, Guid userId)
        {
            var response = await Sender.Send(new DeleteUserInTenantCommand 
            { 
                TenantId = tenantId, 
                UserId = userId 
            });
            
            return Ok(response);
        }

        /// <summary>
        /// Gets a specific tenant with its associated user - available for SysAdmin and Support.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant to retrieve.</param>
        /// <returns>Tenant information with its associated user.</returns>
        /// <remarks>
        /// Requires role SysAdmin or Support.
        /// Returns tenant details and finds the associated user by matching the tenant's email.
        /// </remarks>
        [HttpGet("tenants/{tenantId}")]
        [OpenApiOperation("Get a specific tenant with its associated user")]
        public async Task<IActionResult> GetTenantWithUserAsync(string tenantId)
        {
            var response = await Sender.Send(new GetTenantWithUserQuery { TenantId = tenantId });
            
            return Ok(response);
        }

        /// <summary>
        /// Gets all tenants accessible to the current user.
        /// </summary>
        /// <param name="pageNumber">Page number for pagination (default: 1).</param>
        /// <param name="pageSize">Number of items per page (default: 50).</param>
        /// <returns>List of accessible tenant IDs with pagination info.</returns>
        /// <remarks>
        /// Returns different tenant lists based on the user's role.
        /// For cross-tenant users, returns all tenants. For regular users, returns only their tenant.
        /// </remarks>
        [HttpGet("tenants")]
        [OpenApiOperation("Get accessible tenants for current user")]
        public async Task<IActionResult> GetAccessibleTenantsAsync(int pageNumber = 1, int pageSize = 50)
        {
            var pagination = new PaginationRequest
            {
                Page = pageNumber,
                PageSize = pageSize
            };

            var response = await Sender.Send(new GetAccessibleTenantsQuery { Pagination = pagination });

            return Ok(response);
        }

        /// <summary>
        /// Gets user roles for a specific user in a tenant - available for SysAdmin and Support.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <param name="userId">The ID of the user to get roles for.</param>
        /// <returns>List of user roles.</returns>
        /// <remarks>
        /// Requires role SysAdmin or Support.
        /// </remarks>
        [HttpGet("tenants/{tenantId}/users/{userId}/roles")]
        [OpenApiOperation("Get user roles in a specific tenant")]
        public async Task<IActionResult> GetUserRolesInTenantAsync(string tenantId, Guid userId)
        {
            var response = await Sender.Send(new GetUserRolesInTenantQuery 
            { 
                TenantId = tenantId, 
                UserId = userId 
            });
            
            return Ok(response);
        }

        /// <summary>
        /// Gets all available roles in a specific tenant - available for SysAdmin and Support.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant to get roles from.</param>
        /// <returns>List of available roles.</returns>
        /// <remarks>
        /// Requires role SysAdmin or Support.
        /// </remarks>
        [HttpGet("tenants/{tenantId}/roles")]
        [OpenApiOperation("Get all available roles in a specific tenant")]
        public async Task<IActionResult> GetRolesInTenantAsync(string tenantId)
        {
            var response = await Sender.Send(new GetRolesInTenantQuery { TenantId = tenantId });
            
            return Ok(response);
        }
    }
}
