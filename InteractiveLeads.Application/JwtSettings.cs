namespace InteractiveLeads.Application
{
    /// <summary>
    /// Configuration settings for JWT (JSON Web Token) authentication.
    /// </summary>
    /// <remarks>
    /// Contains all necessary settings for generating and validating JWT tokens
    /// including secret keys, issuer/audience information, and expiration times.
    /// </remarks>
    public sealed class JwtSettings
    {
        /// <summary>
        /// Gets or sets the secret key used for signing and validating JWT tokens.
        /// </summary>
        /// <remarks>
        /// This key should be kept secure and not exposed in source control.
        /// </remarks>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the issuer claim for JWT tokens.
        /// </summary>
        /// <remarks>
        /// Identifies the principal that issued the token.
        /// </remarks>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the audience claim for JWT tokens.
        /// </summary>
        /// <remarks>
        /// Identifies the recipients that the token is intended for.
        /// </remarks>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expiration time for JWT access tokens in minutes.
        /// </summary>
        public int TokenExpiresInMinutes { get; set; }

        /// <summary>
        /// Gets or sets the expiration time for refresh tokens in days.
        /// </summary>
        public int RefreshExpiresInDays { get; set; }
    }
}
