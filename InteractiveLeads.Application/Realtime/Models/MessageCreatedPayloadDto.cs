using InteractiveLeads.Application.Feature.Chat.Messages;

namespace InteractiveLeads.Application.Realtime.Models;

public class MessageCreatedPayloadDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public MessageMediaListItemDto? Media { get; set; }
    public string? SenderId { get; set; }

    /// <summary>When the message event occurred (channel time).</summary>
    public DateTimeOffset MessageDate { get; set; }

    /// <summary>Same as <see cref="MessageDate"/> for clients that still map bubble time from <c>createdAt</c>.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Lowercase: pending, sent, delivered, read, failed.</summary>
    public string Status { get; set; } = "pending";
    public string? MediaProcessingStatus { get; set; }
}

