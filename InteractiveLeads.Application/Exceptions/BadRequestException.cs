using InteractiveLeads.Application.Responses;
using System.Net;

namespace InteractiveLeads.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when the request is invalid (validation error, bad parameter).
    /// Maps to HTTP 400 Bad Request.
    /// </summary>
    public class BadRequestException : Exception
    {
        public HttpStatusCode StatusCode { get; set; }
        public ResultResponse Response { get; private set; }

        public BadRequestException(ResultResponse response, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            StatusCode = statusCode;
            Response = response;
        }
    }
}
