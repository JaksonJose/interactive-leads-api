using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using NSwag;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using System.Reflection;

namespace InteractiveLeads.Infrastructure.OpenApi
{
    /// <summary>
    /// A custom operation processor for adding global authentication support in Swagger/OpenAPI.
    /// </summary>
    /// <param name="scheme">
    /// The authentication scheme to be applied. Defaults to <see cref="JwtBearerDefaults.AuthenticationScheme"/>.
    /// </param>
    public class SwaggerGlobalAuthProcessor(string scheme) : IOperationProcessor
    {
        private readonly string _scheme = scheme;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwaggerGlobalAuthProcessor"/> class
        /// with the default authentication scheme (<see cref="JwtBearerDefaults.AuthenticationScheme"/>).
        /// </summary>
        public SwaggerGlobalAuthProcessor()
            : this(JwtBearerDefaults.AuthenticationScheme)
        {
        }

        /// <summary>
        /// Processes the given operation to conditionally apply security requirements for Swagger documentation.
        /// </summary>
        /// <param name="context">The context containing operation and API metadata.</param>
        /// <returns>
        /// <c>true</c> if the processing is successful; otherwise, <c>false</c>.
        /// This implementation always returns <c>true</c>.
        /// </returns>
        /// <remarks>
        /// The processor checks for the presence of the <see cref="AllowAnonymousAttribute"/> 
        /// in the endpoint's metadata. If present, it skips adding security requirements.
        /// If not present and no security requirements exist, it adds a security requirement
        /// for the specified authentication scheme.
        /// </remarks>
        public bool Process(OperationProcessorContext context)
        {
            IList<object> list = ((AspNetCoreOperationProcessorContext)context)
                .ApiDescription.ActionDescriptor.TryGetPropertyValue<IList<object>>("EndpointMetadata");

            if (list is not null)
            {
                if (list.OfType<AllowAnonymousAttribute>().Any())
                {
                    return true;
                }

                if (context.OperationDescription.Operation.Security is not null && context.OperationDescription.Operation.Security.Count == 0)
                {
                    (context.OperationDescription.Operation.Security ??= [])
                        .Add(new OpenApiSecurityRequirement
                        {
                            {
                                _scheme,
                                Array.Empty<string>()
                            }
                        });
                }
            }
            return true;
        }
    }

    public static class ObjectExtensions
    {
        /// <summary>
        /// Attempts to retrieve the value of a specified property from an object. 
        /// If the property does not exist or is inaccessible, returns the specified default value.
        /// </summary>
        /// <typeparam name="T">The expected type of the property value.</typeparam>
        /// <param name="obj">The object from which to retrieve the property value.</param>
        /// <param name="propertyName">The name of the property to retrieve.</param>
        /// <param name="defaultValue">
        /// The value to return if the property is not found or cannot be accessed. 
        /// Defaults to the default value of type <typeparamref name="T"/>.
        /// </param>
        /// <returns>
        /// The value of the specified property if it exists and is accessible; 
        /// otherwise, the <paramref name="defaultValue"/>.
        /// </returns>
        /// <remarks>
        /// This method uses reflection to dynamically retrieve property values. 
        /// It is best used in scenarios where the type of the object or the properties are not known at compile time.
        /// </remarks>
        public static T TryGetPropertyValue<T>(this object obj, string propertyName, T defaultValue = default) =>
            obj.GetType().GetRuntimeProperty(propertyName) is PropertyInfo propertyInfo
                ? (T)propertyInfo.GetValue(obj)
                : defaultValue;
    }
}
