namespace InteractiveLeads.Application.Feature.Identity.Impersonation
{
    /// <summary>
    /// Request to impersonate a user (SysAdmin/Support only).
    /// </summary>
    public class ImpersonateRequest
    {
        /// <summary>
        /// Id of the user to impersonate.
        /// </summary>
        public Guid UserId { get; set; }
    }
}
