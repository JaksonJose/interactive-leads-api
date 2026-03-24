using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string ExternalMessageId { get; set; } = string.Empty;
    public MessageDirection Direction { get; set; }
    public MessageType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ReplyToMessageId { get; set; }
    public MessageStatus Status { get; set; }
    public string? Metadata { get; set; }
    public Guid? SenderUserId { get; set; }

    /// <summary>When the message event occurred on the channel (from provider payload or send time).</summary>
    public DateTimeOffset MessageDate { get; set; }

    /// <summary>When this row was first persisted in the CRM.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When this row was last updated (status, metadata, media, etc.).</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    public Conversation Conversation { get; set; } = default!;
    public Message? ReplyToMessage { get; set; }
    public ICollection<MessageMedia> Media { get; set; } = new List<MessageMedia>();
    public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
}

