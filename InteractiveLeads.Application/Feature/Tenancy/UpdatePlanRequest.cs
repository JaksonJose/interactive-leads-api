namespace InteractiveLeads.Application.Feature.Tenancy
{
    /// <summary>
    /// Request to update an existing billing plan. When Limits/Features are provided, they replace existing ones.
    /// </summary>
    public sealed class UpdatePlanRequest
    {
        public string? Name { get; set; }
        public string? Identifier { get; set; }
        public bool? IsActive { get; set; }
        /// <summary>If provided, replaces all limits for the plan. Null = leave unchanged.</summary>
        public IReadOnlyDictionary<string, int>? Limits { get; set; }
        /// <summary>If provided, replaces all features for the plan. Null = leave unchanged.</summary>
        public IReadOnlyList<string>? Features { get; set; }
    }
}
