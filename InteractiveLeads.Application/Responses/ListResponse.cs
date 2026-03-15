namespace InteractiveLeads.Application.Responses
{
    /// <summary>
    /// Response wrapper for API operations that return a collection of items.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <remarks>
    /// Used for endpoints that return multiple items (e.g., GET /api/tenants/all).
    /// Extends BaseResponse to include strongly-typed collection data with pagination support.
    /// </remarks>
    public class ListResponse<T> : BaseResponse, IResponse where T : class
    {
        /// <summary>
        /// Gets or sets the collection of items.
        /// </summary>
        public List<T> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the total number of items available (for pagination).
        /// </summary>
        public int AvailableTotalItems { get; set; }

        /// <summary>
        /// Initializes a new instance of the ListResponse class.
        /// </summary>
        public ListResponse() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ListResponse class with items.
        /// </summary>
        /// <param name="items">The items to include in the response.</param>
        public ListResponse(List<T> items) : base()
        {
            this.Items = items;
        }

        /// <summary>
        /// Initializes a new instance of the ListResponse class with items and total count.
        /// </summary>
        /// <param name="items">The items to include in the response.</param>
        /// <param name="totalItems">The total number of items available.</param>
        public ListResponse(List<T> items, int totalItems) : base()
        {
            this.Items = items;
            this.AvailableTotalItems = totalItems;
        }
    }
}
