namespace InteractiveLeads.Application.Feature.Crm.Teams;

public sealed class LinkTeamsToInboxRequest
{
    public List<Guid> TeamIds { get; set; } = new();
}
