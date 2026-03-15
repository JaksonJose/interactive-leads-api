namespace InteractiveLeads.Application.Feature.Identity.Tokens
{
    /// <summary>
    /// Request model for logging out from a specific device.
    /// </summary>
    /// <remarks>
    /// Contains the refresh token that should be revoked to logout from the current device only.
    /// </remarks>
    public class LogoutDeviceRequest
    {
        /// <summary>
        /// Gets or sets the refresh token to revoke for device-specific logout.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;
    }
}
