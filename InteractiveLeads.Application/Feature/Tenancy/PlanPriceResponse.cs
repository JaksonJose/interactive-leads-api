namespace InteractiveLeads.Application.Feature.Tenancy
{
    /// <summary>
    /// Plan price (billing option) as returned by the API.
    /// BillingInterval: 0 = Month, 1 = Year.
    /// </summary>
    public sealed class PlanPriceResponse
    {
        public Guid Id { get; set; }
        public Guid PlanId { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        /// <summary>0 = Month, 1 = Year</summary>
        public int BillingInterval { get; set; }
        public int IntervalCount { get; set; }
        public bool IsActive { get; set; }
    }
}
