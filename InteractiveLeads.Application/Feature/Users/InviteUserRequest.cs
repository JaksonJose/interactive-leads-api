namespace InteractiveLeads.Application.Feature.Users
{
    /// <summary>
    /// Request to invite a user (create without password; they activate via link).
    /// </summary>
    public class InviteUserRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
