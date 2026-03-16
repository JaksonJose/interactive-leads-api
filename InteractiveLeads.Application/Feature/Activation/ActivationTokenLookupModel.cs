namespace InteractiveLeads.Application.Feature.Activation
{
    /// <summary>
    /// Global lookup entry for activation token (host DB). Used to resolve TenantId from token
    /// when activating without tenant context (e.g. dedicated DB per tenant).
    /// </summary>
    public class ActivationTokenLookupModel
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool Used { get; set; }
    }
}
