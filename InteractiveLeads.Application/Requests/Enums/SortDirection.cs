namespace InteractiveLeads.Application.Requests.Enums
{
    /// <summary>
    /// Sort direction for list queries.
    /// </summary>
    public enum SortDirection
    {
        /// <summary>Ascending order (A-Z, 0-9, oldest first).</summary>
        Ascending = 1,

        /// <summary>Descending order (Z-A, 9-0, newest first).</summary>
        Descending = -1
    }
}
