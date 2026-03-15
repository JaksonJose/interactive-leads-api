namespace InteractiveLeads.Infrastructure.OpenApi
{
    /// <summary>
    /// Attribute for adding custom header parameters to Swagger/OpenAPI documentation.
    /// </summary>
    /// <param name="headerName">The name of the header parameter.</param>
    /// <param name="description">Description of the header parameter's purpose.</param>
    /// <param name="defaultValue">Default value for the header parameter.</param>
    /// <param name="isRequired">Indicates whether the header is required.</param>
    /// <remarks>
    /// Apply this attribute to API controller methods to document custom
    /// headers that the endpoint expects in the Swagger UI.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SwaggerHeaderAttribute(string headerName, string description, string defaultValue, bool isRequired) : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the header parameter.
        /// </summary>
        public string HeaderName { get; set; } = headerName;

        /// <summary>
        /// Gets or sets the description of the header parameter.
        /// </summary>
        public string Description { get; set; } = description;

        /// <summary>
        /// Gets or sets the default value for the header parameter.
        /// </summary>
        public string DefaultValue { get; set; } = defaultValue;

        /// <summary>
        /// Gets or sets a value indicating whether the header is required.
        /// </summary>
        public bool IsRequired { get; set; } = isRequired;
    }
}
