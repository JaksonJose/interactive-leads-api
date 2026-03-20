namespace InteractiveLeads.Application.Feature.Chat.Messages;

public sealed class SendConversationMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public string? ExternalMessageId { get; set; }
    public string Type { get; set; } = "text";
    public string? MediaUrl { get; set; }
    public string? Caption { get; set; }
    public string? ReactionEmoji { get; set; }
    public Guid? ReactionMessageId { get; set; }
    public Guid? ReplyToMessageId { get; set; }
}

