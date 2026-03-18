namespace InteractiveLeads.Application.Feature.Chat.Messages;

public sealed class SendConversationMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public string ExternalMessageId { get; set; } = string.Empty;
}

