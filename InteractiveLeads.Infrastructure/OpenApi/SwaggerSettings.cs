namespace InteractiveLeads.Infrastructure.OpenApi
{
    /// <summary>
    /// Configuration settings for Swagger/OpenAPI documentation.
    /// </summary>
    /// <remarks>
    /// Contains metadata used to generate API documentation including
    /// title, description, contact information, and licensing details.
    /// </remarks>
    public class SwaggerSettings
    {
        /// <summary>
        /// Gets or sets the title of the API.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a description of the API.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the contact name for API support.
        /// </summary>
        public string ContactName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the contact email for API support.
        /// </summary>
        public string ContactEmail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the contact URL for API support.
        /// </summary>
        public string ContactUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the API license.
        /// </summary>
        public string LicenseName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL of the API license.
        /// </summary>
        public string LicenseUrl { get; set; } = string.Empty;
    }
}
