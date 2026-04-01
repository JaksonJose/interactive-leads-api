namespace InteractiveLeads.Application.Feature.Chat.Inboxes;

public sealed class CreateInboxRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>Optional team ids to link after creation (same company, active teams only).</summary>
    public List<Guid> TeamIds { get; set; } = new();
}

