using InteractiveLeads.Infrastructure.Constants;
using System.Security.Claims;

namespace InteractiveLeads.Infrastructure.Identity
{
    /// <summary>
    /// Extension methods for ClaimsPrincipal to simplify claim value retrieval.
    /// </summary>
    /// <remarks>
    /// Provides convenient methods to extract common user claims from the
    /// ClaimsPrincipal object without needing to manually search through claims.
    /// </remarks>
    public static class ClaimPrincipalExtensions
    {
        /// <summary>
        /// Gets the email address of the current user from their claims.
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal representing the current user.</param>
        /// <returns>The email address if found, otherwise null.</returns>
        public static string? GetEmail(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.Email);

        /// <summary>
        /// Gets the user ID (unique identifier) of the current user from their claims.
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal representing the current user.</param>
        /// <returns>The user ID if found, otherwise null.</returns>
        public static string? GetUserId(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <summary>
        /// Gets the tenant identifier of the current user from their claims.
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal representing the current user.</param>
        /// <returns>The tenant identifier if found, otherwise null.</returns>
        public static string? GetTenant(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimConstants.Tenant);

        /// <summary>
        /// Gets the first name of the current user from their claims.
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal representing the current user.</param>
        /// <returns>The first name if found, otherwise null.</returns>
        public static string? GetFirstName(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.Name);

        /// <summary>
        /// Gets the last name of the current user from their claims.
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal representing the current user.</param>
        /// <returns>The last name if found, otherwise null.</returns>
        public static string? GetLastName(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.Surname);

        /// <summary>
        /// Gets the phone number of the current user from their claims.
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal representing the current user.</param>
        /// <returns>The phone number if found, otherwise null.</returns>
        public static string? GetPhoneNumber(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.MobilePhone);
    }
}
