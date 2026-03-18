namespace InteractiveLeads.Application.Feature.Chat.Conversations;

public sealed class MoveConversationRequest
{
    public Guid TargetInboxId { get; set; }
    public string? Reason { get; set; }
}

