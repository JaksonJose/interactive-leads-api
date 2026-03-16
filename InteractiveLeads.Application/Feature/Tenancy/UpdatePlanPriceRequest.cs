namespace InteractiveLeads.Application.Feature.Tenancy
{
    /// <summary>
    /// Request to update a plan price. BillingInterval: 0 = Month, 1 = Year.
    /// </summary>
    public sealed class UpdatePlanPriceRequest
    {
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        /// <summary>0 = Month, 1 = Year</summary>
        public int? BillingInterval { get; set; }
        public int? IntervalCount { get; set; }
        public bool? IsActive { get; set; }
    }
}
