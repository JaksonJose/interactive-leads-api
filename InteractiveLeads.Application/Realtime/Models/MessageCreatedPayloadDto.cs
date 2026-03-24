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
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Lowercase: pending, sent, delivered, read, failed.</summary>
    public string Status { get; set; } = "pending";
    public string? MediaProcessingStatus { get; set; }
}

