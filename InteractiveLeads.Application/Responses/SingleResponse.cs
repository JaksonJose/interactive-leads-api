namespace InteractiveLeads.Application.Responses
{
    /// <summary>
    /// Response wrapper for API operations that return a single item.
    /// </summary>
    /// <typeparam name="T">The type of data being returned.</typeparam>
    /// <remarks>
    /// Used for endpoints that return a single object (e.g., GET /api/tenants/{id}).
    /// Extends BaseResponse to include strongly-typed data in addition to success/failure information.
    /// </remarks>
    public class SingleResponse<T> : BaseResponse, IResponse where T : class
    {
        /// <summary>
        /// Gets or sets the data payload of the response.
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Initializes a new instance of the SingleResponse class.
        /// </summary>
        public SingleResponse() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the SingleResponse class with data.
        /// </summary>
        /// <param name="item">The item to include in the response.</param>
        public SingleResponse(T item) : base()
        {
            this.Data = item;
        }
    }
}
