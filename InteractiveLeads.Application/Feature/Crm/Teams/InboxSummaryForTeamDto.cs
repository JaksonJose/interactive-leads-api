namespace InteractiveLeads.Application.Feature.Crm.Teams;

public sealed class InboxSummaryForTeamDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
