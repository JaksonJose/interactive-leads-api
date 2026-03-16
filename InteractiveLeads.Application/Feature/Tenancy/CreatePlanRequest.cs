namespace InteractiveLeads.Application.Feature.Tenancy
{
    /// <summary>
    /// Request to create a new billing plan with optional limits and features.
    /// </summary>
    public sealed class CreatePlanRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        /// <summary>Limit key to value (e.g. users: 10, storage_mb: 100). Optional.</summary>
        public IReadOnlyDictionary<string, int>? Limits { get; set; }
        /// <summary>Feature keys (e.g. whatsapp_integration, api_access). Optional.</summary>
        public IReadOnlyList<string>? Features { get; set; }
    }
}
