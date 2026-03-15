namespace InteractiveLeads.Application.Responses
{
    /// <summary>
    /// Response wrapper for API operations that don't return data (e.g., DELETE, PUT operations).
    /// </summary>
    /// <remarks>
    /// Used for endpoints that perform operations without returning data payload.
    /// Examples: POST /api/tenants/{id}/activate, DELETE /api/tenants/{id}
    /// Extends BaseResponse to provide operation status and messages.
    /// </remarks>
    public class ResultResponse : BaseResponse, IResponse
    {
        /// <summary>
        /// Initializes a new instance of the ResultResponse class.
        /// </summary>
        public ResultResponse() : base()
        {
        }
    }
}
