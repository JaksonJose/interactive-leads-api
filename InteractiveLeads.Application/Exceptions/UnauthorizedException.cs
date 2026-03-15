using InteractiveLeads.Application.Responses;
using System.Net;

namespace InteractiveLeads.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when authentication fails or is required but not provided.
    /// </summary>
    /// <remarks>
    /// This exception maps to HTTP 401 Unauthorized status code and indicates that
    /// the user must authenticate before accessing the resource. This is different from
    /// ForbiddenException (403) which indicates the user is authenticated but lacks permissions.
    /// </remarks>
    public class UnauthorizedException : Exception
    {
        /// <summary>
        /// Gets or sets the HTTP status code associated with this exception.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        public ResultResponse Response { get; private set; }

        public UnauthorizedException(ResultResponse response = default!, HttpStatusCode statusCode = HttpStatusCode.Unauthorized)
        {
            StatusCode = statusCode;
            Response = response;
        }
    }
}
