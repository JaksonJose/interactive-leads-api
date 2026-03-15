using InteractiveLeads.Application.Responses.Messages;

namespace InteractiveLeads.Application.Responses
{
    /// <summary>
    /// Interface for wrapping API responses with standard success/failure information and messages.
    /// </summary>
    /// <remarks>
    /// Provides a consistent response structure across all API endpoints.
    /// </remarks>
    public interface IResponse
    {
        /// <summary>
        /// Gets or sets the list of messages (errors or informational) associated with the response.
        /// </summary>
        List<Message> Messages { get; set; }
    }
}
