namespace InteractiveLeads.Infrastructure.Tenancy.Models
{
    /// <summary>
    /// Feature key enabled for a plan (e.g. whatsapp_integration, api_access). All features come from DB.
    /// </summary>
    public sealed class PlanFeature
    {
        public Guid Id { get; set; }
        public Guid PlanId { get; set; }
        public Plan Plan { get; set; } = null!;
        public string FeatureKey { get; set; } = string.Empty;
    }
}
