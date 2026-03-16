namespace InteractiveLeads.Infrastructure.Tenancy.Models
{
    /// <summary>
    /// Represents a billing plan in the catalog. Limits and features are stored in PlanLimits and PlanFeatures.
    /// </summary>
    public sealed class Plan
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        /// <summary>Unique code/slug for the plan (e.g. "free", "pro").</summary>
        public string Identifier { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
