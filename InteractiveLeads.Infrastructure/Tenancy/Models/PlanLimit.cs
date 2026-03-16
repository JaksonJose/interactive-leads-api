namespace InteractiveLeads.Infrastructure.Tenancy.Models
{
    /// <summary>
    /// Key-value limit for a plan (e.g. users, whatsapp_numbers, leads, storage_mb). All limits come from DB.
    /// </summary>
    public sealed class PlanLimit
    {
        public Guid Id { get; set; }
        public Guid PlanId { get; set; }
        public Plan Plan { get; set; } = null!;
        public string LimitKey { get; set; } = string.Empty;
        public int LimitValue { get; set; }
    }
}
