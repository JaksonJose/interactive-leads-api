using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Feature.Crm.Teams;

public sealed class AddUserToTeamRequest
{
    public string UserId { get; set; } = string.Empty;
    public TeamMemberRole? Role { get; set; }
}
