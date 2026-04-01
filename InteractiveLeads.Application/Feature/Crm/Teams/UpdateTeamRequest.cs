using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Feature.Crm.Teams;

public sealed class UpdateTeamRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }

    public Guid? CalendarId { get; set; }

    public Guid? SlaPolicyId { get; set; }

    public bool? AutoAssignEnabled { get; set; }

    public AutoAssignStrategy? AutoAssignStrategy { get; set; }

    public bool? AutoAssignIgnoreOfflineUsers { get; set; }

    public int? AutoAssignMaxConversationsPerUser { get; set; }

    public int? AutoAssignReassignTimeoutMinutes { get; set; }
}
