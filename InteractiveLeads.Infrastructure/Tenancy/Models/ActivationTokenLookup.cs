namespace InteractiveLeads.Infrastructure.Tenancy.Models
{
    /// <summary>
    /// Global lookup for activation tokens (host DB). Maps token -> TenantId so the public
    /// activation endpoint can resolve which tenant DB to use when tenants have dedicated databases.
    /// </summary>
    public sealed class ActivationTokenLookup
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool Used { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
