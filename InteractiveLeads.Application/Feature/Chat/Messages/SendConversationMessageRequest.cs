namespace InteractiveLeads.Application.Feature.Chat.Messages;

public sealed class SendConversationMessageRequest
{
    public string Content { get; set; } = string.Empty;

    /// <summary>Optional UNIX seconds for message event time when sending from a client clock.</summary>
    public long? ClientTimestamp { get; set; }

    public string? ExternalMessageId { get; set; }
    public string Type { get; set; } = "text";
    public string? MediaUrl { get; set; }
    public string? Caption { get; set; }
    public string? ReactionEmoji { get; set; }
    public Guid? ReactionMessageId { get; set; }
    public Guid? ReplyToMessageId { get; set; }
}

