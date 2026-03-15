namespace InteractiveLeads.Application.Feature.Identity.Tokens
{
    /// <summary>
    /// Response model containing JWT authentication tokens.
    /// </summary>
    /// <remarks>
    /// Returned after successful authentication or token refresh operations.
    /// Contains both the JWT access token and a refresh token for obtaining new access tokens.
    /// </remarks>
    public class TokenResponse
    {
        /// <summary>
        /// Gets or sets the JWT access token used for authenticating API requests.
        /// </summary>
        public string Jwt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the refresh token used to obtain a new JWT when the current one expires.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expiration date of the refresh token.
        /// </summary>
        public DateTime RefreshTokenExpirationDate { get; set; }   
    }
}
