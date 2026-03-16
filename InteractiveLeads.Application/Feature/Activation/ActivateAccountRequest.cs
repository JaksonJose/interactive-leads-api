namespace InteractiveLeads.Application.Feature.Activation
{
    /// <summary>
    /// Request to activate an account using the token from the invitation link.
    /// </summary>
    public class ActivateAccountRequest
    {
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
