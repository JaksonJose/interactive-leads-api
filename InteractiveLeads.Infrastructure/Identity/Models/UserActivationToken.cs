using System;

namespace InteractiveLeads.Infrastructure.Identity.Models
{
    /// <summary>
    /// One-time activation token for invited users.
    /// </summary>
    public class UserActivationToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool Used { get; set; }
        public DateTime CreatedAt { get; set; }

        public ApplicationUser User { get; set; } = null!;
    }
}

