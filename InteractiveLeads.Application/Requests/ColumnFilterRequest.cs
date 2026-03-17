namespace InteractiveLeads.Application.Requests
{
    /// <summary>
    /// Represents a single column filter for list queries.
    /// Matches PrimeNG FilterMetadata (field → key, matchMode, value).
    /// </summary>
    public sealed class ColumnFilterRequest
    {
        /// <summary>
        /// Column/field name (e.g. name, email, firstName, lastName, expirationDate, isActive).
        /// </summary>
        public string? Field { get; set; }

        /// <summary>
        /// Match mode: contains, startsWith, endsWith, equals, notEquals, in, lt, lte, gt, gte, dateIs, dateIsNot, dateBefore, dateAfter.
        /// </summary>
        public string? MatchMode { get; set; }

        /// <summary>
        /// Filter value. For dates use ISO string; for boolean use "true"/"false".
        /// </summary>
        public string? Value { get; set; }
    }
}