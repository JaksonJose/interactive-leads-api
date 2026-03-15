using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.Identity.Tokens;
using InteractiveLeads.Application.Feature.Identity.Tokens.Commands;
using InteractiveLeads.Application.Feature.Identity.Tokens.Queries;
using InteractiveLeads.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers
{
    /// <summary>
    /// Controller for handling JWT token operations including login and token refresh.
    /// </summary>
    /// <remarks>
    /// Provides endpoints for user authentication and JWT token management.
    /// </remarks>
    public class TokenController : BaseApiController
    {
        /// <summary>
        /// Authenticates a user and returns a JWT access token and refresh token.
        /// </summary>
        /// <param name="tokenRequest">The login credentials containing username/email and password.</param>
        /// <returns>Returns Ok with JWT tokens if authentication is successful, otherwise BadRequest.</returns>
        /// <remarks>
        /// This endpoint allows anonymous access for initial login.
        /// The tenant is automatically resolved from the user's email address.
        /// </remarks>
        [HttpPost("login")]
        [AllowAnonymous]
        [OpenApiOperation("Used to obtain jwt for login")]
        public async Task<IActionResult> GetTokenAsync([FromBody] TokenRequest tokenRequest)
        {
            var response = await Sender.Send(new GetTokenQuery { TokenRequest = tokenRequest });
            return Ok(response);
        }

        /// <summary>
        /// Generates a new JWT access token using a valid refresh token.
        /// </summary>
        /// <param name="refreshTokenRequest">The request containing the refresh token.</param>
        /// <returns>Returns Ok with new JWT tokens if the refresh token is valid, otherwise BadRequest.</returns>
        /// <remarks>
        /// This endpoint allows anonymous access for token refresh.
        /// This allows users to obtain a new access token without re-authenticating.
        /// </remarks>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [OpenApiOperation("Used to generate new jwt from refresh token")]
        public async Task<IActionResult> GetRefreshTokenAsync([FromBody] RefreshTokenRequest refreshTokenRequest)
        {
            var response = await Sender.Send(new GetRefreshTokenQuery { RefreshToken = refreshTokenRequest });
            return Ok(response);
        }

        /// <summary>
        /// Logs out the current user by revoking all their refresh tokens from all devices.
        /// </summary>
        /// <returns>Returns Ok if logout is successful.</returns>
        /// <remarks>
        /// This endpoint requires authentication and revokes all refresh tokens for the current user.
        /// After calling this endpoint, the user will need to log in again on all devices.
        /// </remarks>
        [HttpPost("logout-all")]
        [OpenApiOperation("Used to logout user from all devices and revoke all refresh tokens")]
        public async Task<IActionResult> LogoutFromAllDevicesAsync()
        {
            string? userIdString = User.GetUserId();
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return BadRequest("User ID not found in token.");
            }

            var response = await Sender.Send(new LogoutCommand { UserId = userId });
            return Ok(response);
        }

        /// <summary>
        /// Logs out the current user from the current device only by revoking the specific refresh token.
        /// </summary>
        /// <param name="logoutDeviceRequest">The request containing the refresh token to revoke.</param>
        /// <returns>Returns Ok if logout is successful.</returns>
        /// <remarks>
        /// This endpoint requires authentication and revokes only the specified refresh token.
        /// The user will remain logged in on other devices.
        /// </remarks>
        [HttpPost("logout-device")]
        [OpenApiOperation("Used to logout user from current device only")]
        public async Task<IActionResult> LogoutFromCurrentDeviceAsync([FromBody] LogoutDeviceRequest logoutDeviceRequest)
        {
            string? userIdString = User.GetUserId();
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return BadRequest("User ID not found in token.");
            }

            var response = await Sender.Send(new LogoutDeviceCommand 
            { 
                UserId = userId, 
                RefreshToken = logoutDeviceRequest.RefreshToken 
            });
            return Ok(response);
        }
    }
}
