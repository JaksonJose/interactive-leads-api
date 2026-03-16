namespace InteractiveLeads.Application.Feature.Activation
{
    /// <summary>
    /// Response after creating an invitation; contains the activation URL for development/testing.
    /// </summary>
    public class InviteUserResponse
    {
        public Guid UserId { get; set; }
        public string ActivationUrl { get; set; } = string.Empty;
    }
}
