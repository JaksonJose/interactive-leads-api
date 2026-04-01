namespace InteractiveLeads.Application.Feature.Chat.Inboxes;

public sealed class UpdateInboxRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    /// <summary>Replaces all team links for this inbox (same company, active teams only).</summary>
    public List<Guid> TeamIds { get; set; } = new();
}

