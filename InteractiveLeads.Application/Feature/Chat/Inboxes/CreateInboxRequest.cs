namespace InteractiveLeads.Application.Feature.Chat.Inboxes;

public sealed class CreateInboxRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>Ordered team ids (routing priority: first = tried first). At least one required.</summary>
    public List<Guid> TeamIds { get; set; } = new();

    public Guid? DefaultCalendarId { get; set; }

    public Guid? DefaultSlaPolicyId { get; set; }
}

