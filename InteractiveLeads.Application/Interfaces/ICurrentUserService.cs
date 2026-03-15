namespace InteractiveLeads.Application.Interfaces
{
    /// <summary>
    /// Service interface for accessing current user information.
    /// </summary>
    /// <remarks>
    /// Provides methods to access the currently authenticated user's identity.
    /// </remarks>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Gets the name of the current user.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the unique identifier of the current user.
        /// </summary>
        /// <returns>User identifier as string.</returns>
        string GetUserId();

        /// <summary>
        /// Gets the email address of the current user.
        /// </summary>
        /// <returns>User email address.</returns>
        string GetUserEmail();

        /// <summary>
        /// Gets the tenant identifier of the current user.
        /// </summary>
        /// <returns>Tenant identifier.</returns>
        string GetUserTenant();

        /// <summary>
        /// Checks if the current user is authenticated.
        /// </summary>
        /// <returns>True if user is authenticated, false otherwise.</returns>
        bool IsAuthenticated();

        /// <summary>
        /// Checks if the current user is in the specified role.
        /// </summary>
        /// <param name="roleName">Name of the role to check.</param>
        /// <returns>True if user is in the role, false otherwise.</returns>
        bool IsInRole(string roleName);

    }
}
