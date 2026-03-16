namespace InteractiveLeads.Application.Feature.Tenancy
{
    /// <summary>
    /// Request to create a plan price (billing option). BillingInterval: 0 = Month, 1 = Year.
    /// </summary>
    public sealed class CreatePlanPriceRequest
    {
        public decimal Price { get; set; }
        public string Currency { get; set; } = "BRL";
        /// <summary>0 = Month, 1 = Year</summary>
        public int BillingInterval { get; set; }
        public int IntervalCount { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }
}
