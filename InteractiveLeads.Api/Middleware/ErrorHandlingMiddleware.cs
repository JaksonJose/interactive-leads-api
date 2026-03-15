using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Responses;
using System.Net;
using System.Text.Json;

namespace InteractiveLeads.Api.Middleware
{
    /// <summary>
    /// Middleware for global exception handling and error response formatting.
    /// </summary>
    /// <remarks>
    /// Catches all exceptions thrown during request processing and converts them
    /// into appropriate HTTP responses with consistent error formatting.
    /// </remarks>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the ErrorHandlingMiddleware.
        /// </summary>
        /// <param name="next">The next middleware in the request pipeline.</param>
        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Processes the HTTP request and handles any exceptions that occur.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// Maps custom exceptions to appropriate HTTP status codes and formats error responses.
        /// Unhandled exceptions are mapped to 500 Internal Server Error.
        /// </remarks>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex) 
            {
                var response = context.Response;
                response.ContentType = "application/json";

                var responseWrapper = HandleException(ex);
                response.StatusCode = (int)GetStatusCode(ex);

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var result = JsonSerializer.Serialize(responseWrapper, jsonOptions);

                await response.WriteAsync(result);
            }
        }

        /// <summary>
        /// Handles the exception and returns an appropriate ResponseWrapper.
        /// </summary>
        /// <param name="ex">The exception to handle.</param>
        /// <returns>A ResponseWrapper with error information.</returns>
        private static IResponse HandleException(Exception ex)
        {
            return ex switch
            {
                ConflictException ce when ce.Response != null => ce.Response,
                ConflictException => new ResultResponse().AddErrorMessage("Conflict detected", "general.conflict"),
                
                NotFoundException nfe when nfe.Response != null => nfe.Response,
                NotFoundException => new ResultResponse().AddErrorMessage("Resource not found", "general.resource_not_found"),
                
                ForbiddenException fe when fe.Response != null => fe.Response,
                ForbiddenException => new ResultResponse().AddErrorMessage("Access denied", "general.access_denied"),
                
                IdentityException ie when ie.Response != null => ie.Response,
                IdentityException => new ResultResponse().AddErrorMessage("Identity error", "identity.permission_denied"),
                
                UnauthorizedException ue when ue.Response != null => ue.Response,
                UnauthorizedException => new ResultResponse().AddErrorMessage("Unauthorized", "general.unauthorized"),
                
                _ => new ResultResponse().AddErrorMessage("Something went wrong", "general.something_went_wrong")
            };
        }

        /// <summary>
        /// Gets the appropriate HTTP status code for the exception.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <returns>The HTTP status code.</returns>
        private static HttpStatusCode GetStatusCode(Exception ex)
        {
            return ex switch
            {
                ConflictException ce => ce.StatusCode,
                NotFoundException nfe => nfe.StatusCode,
                ForbiddenException fe => fe.StatusCode,
                IdentityException ie => ie.StatusCode,
                UnauthorizedException ue => ue.StatusCode,
                _ => HttpStatusCode.InternalServerError
            };
        }
    }
}
