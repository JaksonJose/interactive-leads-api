using InteractiveLeads.Infrastructure.Constants;

namespace InteractiveLeads.Infrastructure.OpenApi
{
    /// <summary>
    /// Attribute for documenting the tenant header requirement in Swagger/OpenAPI.
    /// </summary>
    /// <remarks>
    /// This is a specialized header attribute that automatically configures
    /// the tenant identifier header for multi-tenant API endpoints.
    /// Apply this to controller methods that require tenant context.
    /// </remarks>
    public class TenantHeaderAttribute() 
        : SwaggerHeaderAttribute(
            headerName: TenancyConstants.TenantIdName, 
            description: "Enter your tenant name to access this API", 
            defaultValue: string.Empty, 
            isRequired: true)
    {
    }
}
