using InteractiveLeads.Application.Responses;
using System.Net;

namespace InteractiveLeads.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when a conflict occurs during an operation.
    /// </summary>
    /// <remarks>
    /// This exception is typically used for scenarios such as duplicate entries,
    /// concurrent modifications, or resource conflicts. It maps to HTTP 409 Conflict status code.
    /// </remarks>
    public class ConflictException : Exception
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
        /// Initializes a new instance of the ConflictException class.
        /// </summary>
        /// <param name="response">The response wrapper containing error information.</param>
        /// <param name="statusCode">The HTTP status code (defaults to Conflict).</param>
        public ConflictException(ResultResponse response = default!, HttpStatusCode statusCode = HttpStatusCode.Conflict)
        {
            StatusCode = statusCode;
            Response = response;
        }
    }
}
