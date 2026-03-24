namespace InteractiveLeads.Application.Feature.Chat.Messages;

public sealed class MessageListItemDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public MessageMediaListItemDto? Media { get; set; }
    public string Direction { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Lowercase: pending, sent, delivered, read, failed.</summary>
    public string Status { get; set; } = "pending";
    public string? MediaProcessingStatus { get; set; }
}

public sealed class MessageMediaListItemDto
{
    public string Url { get; set; } = string.Empty;
    public string? OptimizedUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string? Caption { get; set; }
}

