using System.Net;

namespace InteractiveLeads.Application.Responses
{
    public sealed record RawHttpResult
    {
        public string? Content { get; }
        public HttpStatusCode StatusCode { get; }

        public RawHttpResult(string? content, HttpStatusCode statusCode)
        {
            Content = content ?? string.Empty;
            StatusCode = statusCode;
        }
    }
}
