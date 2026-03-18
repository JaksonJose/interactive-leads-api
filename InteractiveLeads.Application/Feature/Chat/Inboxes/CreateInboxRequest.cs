namespace InteractiveLeads.Application.Feature.Chat.Inboxes;

public sealed class CreateInboxRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

