using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Domain.Entities;

public class UserTeam
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid TeamId { get; set; }
    public TeamMemberRole? Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; }

    public Team Team { get; set; } = default!;
}
