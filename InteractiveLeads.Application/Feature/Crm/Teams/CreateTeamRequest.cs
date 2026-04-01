namespace InteractiveLeads.Application.Feature.Crm.Teams;

public sealed class CreateTeamRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
