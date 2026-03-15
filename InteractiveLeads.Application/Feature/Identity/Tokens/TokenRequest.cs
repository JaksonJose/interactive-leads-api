namespace InteractiveLeads.Application.Feature.Identity.Tokens
{
    /// <summary>
    /// Request model for user authentication and token generation.
    /// </summary>
    /// <remarks>
    /// Used to authenticate a user with username and password to obtain JWT tokens.
    /// </remarks>
    public class TokenRequest
    {
        /// <summary>
        /// Gets or sets the username for authentication.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password for authentication.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}
