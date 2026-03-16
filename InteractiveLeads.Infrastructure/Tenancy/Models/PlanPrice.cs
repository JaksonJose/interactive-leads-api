namespace InteractiveLeads.Infrastructure.Tenancy.Models
{
    /// <summary>
    /// Billing option for a plan (e.g. monthly, quarterly, yearly). Prices are never hardcoded.
    /// </summary>
    public sealed class PlanPrice
    {
        public Guid Id { get; set; }
        public Guid PlanId { get; set; }
        public Plan Plan { get; set; } = null!;
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public BillingInterval BillingInterval { get; set; }
        public int IntervalCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
