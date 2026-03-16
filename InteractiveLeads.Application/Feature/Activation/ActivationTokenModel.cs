namespace InteractiveLeads.Application.Feature.Activation
{
    /// <summary>
    /// Application-layer model for an activation token.
    /// </summary>
    public class ActivationTokenModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool Used { get; set; }
    }
}
