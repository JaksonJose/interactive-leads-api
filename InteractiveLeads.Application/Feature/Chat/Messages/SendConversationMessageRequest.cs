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
    public string? MimeType { get; set; }
    public string? FileName { get; set; }
    /// <summary>After image pipeline: original WebP URL (CRM / audit).</summary>
    public string? MediaOriginalUrl { get; set; }
    /// <summary>After image pipeline: thumbnail WebP URL for light UI loads.</summary>
    public string? MediaThumbnailUrl { get; set; }
    /// <summary>WhatsApp voice note when true; file audio when false.</summary>
    public bool? Voice { get; set; }
    public string? ReactionEmoji { get; set; }
    public Guid? ReactionMessageId { get; set; }
    public Guid? ReplyToMessageId { get; set; }
}

