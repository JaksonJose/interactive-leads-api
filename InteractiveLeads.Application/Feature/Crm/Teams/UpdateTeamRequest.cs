namespace InteractiveLeads.Application.Feature.Crm.Teams;

public sealed class UpdateTeamRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
