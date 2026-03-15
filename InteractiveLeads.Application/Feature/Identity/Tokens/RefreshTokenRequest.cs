namespace InteractiveLeads.Application.Feature.Identity.Tokens
{
    /// <summary>
    /// Request model for refreshing an expired JWT access token.
    /// </summary>
    /// <remarks>
    /// Contains the current tokens that need to be refreshed to obtain new tokens
    /// without requiring the user to re-authenticate.
    /// </remarks>
    public class RefreshTokenRequest
    {
        /// <summary>
        /// Gets or sets the current (potentially expired) JWT access token.
        /// </summary>
        public string CurrentJwt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current refresh token used to obtain a new JWT.
        /// </summary>
        public string CurrentRefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expiration date of the refresh token.
        /// </summary>
        public DateTime RefreshTokenExpiryDate { get; set; }
    }
}
