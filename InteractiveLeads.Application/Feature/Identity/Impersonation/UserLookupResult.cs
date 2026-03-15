namespace InteractiveLeads.Application.Feature.Identity.Impersonation
{
    /// <summary>
    /// Result of looking up a user by id (e.g. for impersonation). Contains data needed to build JWT claims.
    /// </summary>
    public class UserLookupResult
    {
        public Guid Id { get; set; }
        public string? TenantId { get; set; }
        public string? Email { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    }
}
