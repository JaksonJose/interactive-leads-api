namespace InteractiveLeads.Application.Models
{
    /// <summary>
    /// Request model for list pagination.
    /// </summary>
    /// <remarks>
    /// Used to control pagination of results in endpoints that return lists.
    /// </remarks>
    public sealed class PaginationRequest
    {
        /// <summary>
        /// Page number to be returned (1-based).
        /// </summary>
        /// <remarks>
        /// Default value: 1
        /// </remarks>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Number of items per page.
        /// </summary>
        /// <remarks>
        /// Default value: 10
        /// Maximum value: 100
        /// </remarks>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Calculates the number of items to skip based on the current page.
        /// </summary>
        /// <returns>Number of items to skip.</returns>
        public int CalculateSkip()
        {
            return (Page - 1) * PageSize;
        }

        /// <summary>
        /// Validates the pagination parameters.
        /// </summary>
        /// <returns>True if the parameters are valid, False otherwise.</returns>
        public bool IsValid()
        {
            return Page > 0 && PageSize > 0 && PageSize <= 100;
        }
    }
}
