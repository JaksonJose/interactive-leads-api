using InteractiveLeads.Application.Responses;
using System.Net;

namespace InteractiveLeads.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when a requested resource cannot be found.
    /// </summary>
    /// <remarks>
    /// This exception maps to HTTP 404 Not Found status code and indicates that
    /// the requested resource does not exist in the system.
    /// </remarks>
    public class NotFoundException : Exception
    {
        /// <summary>
        /// Gets or sets the HTTP status code associated with this exception.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the response wrapper containing error information.
        /// </summary>
        public ResultResponse Response { get; private set; }

        /// <summary>
        /// Initializes a new instance of the NotFoundException class.
        /// </summary>
        /// <param name="response">The response wrapper containing error information.</param>
        /// <param name="statusCode">The HTTP status code (defaults to NotFound).</param>
        public NotFoundException(ResultResponse response = default!, HttpStatusCode statusCode = HttpStatusCode.NotFound) 
        {
            StatusCode = statusCode;
            Response = response;
        }
    }
}
