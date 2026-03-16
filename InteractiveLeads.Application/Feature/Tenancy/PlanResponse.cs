namespace InteractiveLeads.Application.Feature.Tenancy
{
    /// <summary>
    /// Billing plan as returned by the API. Limits and features come from DB.
    /// </summary>
    public sealed class PlanResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        /// <summary>Limit key to value. Optional; populated when including details.</summary>
        public IReadOnlyDictionary<string, int>? Limits { get; set; }
        /// <summary>Feature keys. Optional; populated when including details.</summary>
        public IReadOnlyList<string>? Features { get; set; }
    }
}
