using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Feature.Crm.Teams;

public sealed class TeamDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CalendarId { get; set; }
    public Guid? SlaPolicyId { get; set; }
    /// <summary>Active user count on the team (UserTeam rows).</summary>
    public int MemberCount { get; set; }

    /// <summary>When listing teams for an inbox: routing order (1 = highest priority).</summary>
    public int? RoutingPriority { get; set; }

    public bool AutoAssignEnabled { get; set; }

    public AutoAssignStrategy AutoAssignStrategy { get; set; }

    public bool AutoAssignIgnoreOfflineUsers { get; set; }

    public int? AutoAssignMaxConversationsPerUser { get; set; }

    public int? AutoAssignReassignTimeoutMinutes { get; set; }
}

public sealed class TeamMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public TeamMemberRole? Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
}
