using InteractiveLeads.Application.Feature.Chat.Messages;

namespace InteractiveLeads.Application.Realtime.Models;

public class MessageCreatedPayloadDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }

    /// <summary>Optional; lets inbox UIs show contact title when the row was created before HTTP reload.</summary>
    public Guid? ContactId { get; set; }

    public string? ContactName { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public MessageMediaListItemDto? Media { get; set; }
    public string? SenderId { get; set; }
    /// <summary>Client idempotency id (same as <see cref="Domain.Entities.Message.ExternalMessageId"/>).</summary>
    public string? ExternalMessageId { get; set; }

    /// <summary>When the message event occurred (channel time).</summary>
    public DateTimeOffset MessageDate { get; set; }

    /// <summary>Same as <see cref="MessageDate"/> for clients that still map bubble time from <c>createdAt</c>.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Lowercase: pending, sent, delivered, read, failed.</summary>
    public string Status { get; set; } = "pending";
    public string? MediaProcessingStatus { get; set; }

    public MessageTemplateSnapshotDto? TemplateSnapshot { get; set; }
}

