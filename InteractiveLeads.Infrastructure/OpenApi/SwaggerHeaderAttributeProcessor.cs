using NJsonSchema;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using System.Reflection;

namespace InteractiveLeads.Infrastructure.OpenApi
{
    /// <summary>
    /// Operation processor that adds custom header parameters to Swagger documentation
    /// based on SwaggerHeaderAttribute decorations.
    /// </summary>
    /// <remarks>
    /// Scans controller methods for SwaggerHeaderAttribute and adds the specified
    /// headers to the Swagger/OpenAPI documentation for those endpoints.
    /// </remarks>
    public class SwaggerHeaderAttributeProcessor : IOperationProcessor
    {
        /// <summary>
        /// Processes the operation to add custom header parameters from SwaggerHeaderAttribute.
        /// </summary>
        /// <param name="context">The operation processor context containing method metadata.</param>
        /// <returns>True if processing succeeded, otherwise false.</returns>
        public bool Process(OperationProcessorContext context)
        {
            if (context.MethodInfo.GetCustomAttribute(typeof(SwaggerHeaderAttribute)) is SwaggerHeaderAttribute swaggerHeader)
            {
                var parameters = context.OperationDescription.Operation.Parameters;

                var existstingParam = parameters
                    .FirstOrDefault(p => p.Kind == OpenApiParameterKind.Header && p.Name == swaggerHeader.HeaderName);

                if (existstingParam is not null)
                {
                    parameters.Remove(existstingParam);
                }

                parameters.Add(new OpenApiParameter
                {
                    Name = swaggerHeader.HeaderName,
                    Kind = OpenApiParameterKind.Header,
                    Description = swaggerHeader.Description,
                    IsRequired = true,
                    Schema = new JsonSchema
                    {
                        Type = JsonObjectType.String,
                        Default = swaggerHeader.DefaultValue,
                    }
                });
            }

            return true;
        }
    }
}
