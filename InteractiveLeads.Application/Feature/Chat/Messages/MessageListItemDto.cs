namespace InteractiveLeads.Application.Feature.Chat.Messages;

public sealed class MessageListItemDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Lowercase: pending, sent, delivered, read, failed.</summary>
    public string Status { get; set; } = "pending";
}

