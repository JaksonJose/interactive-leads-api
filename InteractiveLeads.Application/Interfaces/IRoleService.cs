using InteractiveLeads.Application.Feature.Identity.Roles;
using InteractiveLeads.Application.Responses;

namespace InteractiveLeads.Application.Interfaces
{
    /// <summary>
    /// Service interface for role management operations.
    /// </summary>
    /// <remarks>
    /// Provides methods for creating, updating, deleting and managing roles in the system.
    /// </remarks>
    public interface IRoleService
    {
        /// <summary>
        /// Creates a new role in the system.
        /// </summary>
        /// <param name="request">Role creation request data.</param>
        /// <param name="ct">Cancellation token for the async operation.</param>
        /// <returns>Result of the role creation operation.</returns>
        Task<ResultResponse> CreateAsync(CreateRoleRequest request, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing role.
        /// </summary>
        /// <param name="request">Role update request data.</param>
        /// <param name="ct">Cancellation token for the async operation.</param>
        /// <returns>Result of the role update operation.</returns>
        Task<ResultResponse> UpdateAsync(UpdateRoleRequest request, CancellationToken ct = default);

        /// <summary>
        /// Deletes a role from the system.
        /// </summary>
        /// <param name="id">Unique identifier of the role to delete.</param>
        /// <param name="ct">Cancellation token for the async operation.</param>
        /// <returns>Result of the role deletion operation.</returns>
        Task<ResultResponse> DeleteAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Checks if a role with the specified name exists.
        /// </summary>
        /// <param name="name">Name of the role to check.</param>
        /// <param name="ct">Cancellation token for the async operation.</param>
        /// <returns>True if role exists, false otherwise.</returns>
        Task<bool> DoesItExistsAsync(string name, CancellationToken ct = default);

        /// <summary>
        /// Retrieves all roles in the system.
        /// </summary>
        /// <param name="ct">Cancellation token for the async operation.</param>
        /// <returns>List of all roles.</returns>
        Task<ListResponse<RoleResponse>> GetAllAsync(CancellationToken ct);

        /// <summary>
        /// Retrieves a role by its unique identifier.
        /// </summary>
        /// <param name="id">Unique identifier of the role.</param>
        /// <param name="ct">Cancellation token for the async operation.</param>
        /// <returns>Role data if found.</returns>
        Task<SingleResponse<RoleResponse>> GetByIdAsync(Guid id, CancellationToken ct);

        /// <summary>
        /// Retrieves a role by its unique identifier including permissions.
        /// </summary>
        /// <param name="id">Unique identifier of the role.</param>
        /// <param name="ct">Cancellation token for the async operation.</param>
        /// <returns>Role data with permissions if found.</returns>
        Task<SingleResponse<RoleResponse>> GetRoleWithPermissionsAsync(Guid id, CancellationToken ct);
    }
}
