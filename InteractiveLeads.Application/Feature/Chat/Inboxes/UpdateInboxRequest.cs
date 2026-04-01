namespace InteractiveLeads.Application.Feature.Chat.Inboxes;

public sealed class UpdateInboxRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    /// <summary>Replaces all team links; order = routing priority (first = tried first). At least one required.</summary>
    public List<Guid> TeamIds { get; set; } = new();

    public Guid? DefaultCalendarId { get; set; }

    public Guid? DefaultSlaPolicyId { get; set; }
}

