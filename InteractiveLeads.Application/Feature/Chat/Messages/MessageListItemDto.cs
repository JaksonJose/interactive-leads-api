namespace InteractiveLeads.Application.Feature.Chat.Messages;

/// <summary>Rich template layout for chat UI (root <c>templateSnapshot</c> on <see cref="Domain.Entities.Message.Metadata"/>; preserved when metadata is patched).</summary>
public sealed class MessageTemplateButtonSnapshotDto
{
    public string Type { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string? PhoneNumber { get; set; }
}

public sealed class MessageTemplateSnapshotDto
{
    public string? HeaderText { get; set; }

    public string BodyText { get; set; } = string.Empty;

    public string? FooterText { get; set; }

    public IReadOnlyList<MessageTemplateButtonSnapshotDto>? Buttons { get; set; }
}

public sealed class MessageListItemDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public MessageMediaListItemDto? Media { get; set; }
    public string Direction { get; set; } = string.Empty;

    /// <summary>When the message event occurred (channel time).</summary>
    public DateTimeOffset MessageDate { get; set; }

    /// <summary>When this message row was first saved in the CRM.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When this message row was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Lowercase: pending, sent, delivered, read, failed.</summary>
    public string Status { get; set; } = "pending";
    public string? MediaProcessingStatus { get; set; }

    /// <summary>Set when <see cref="Type"/> is <c>template</c> and metadata contains a snapshot.</summary>
    public MessageTemplateSnapshotDto? TemplateSnapshot { get; set; }
}

public sealed class MessageMediaListItemDto
{
    public string Url { get; set; } = string.Empty;
    public string? OptimizedUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string? FileName { get; set; }

    /// <summary>Sticker: animated when true.</summary>
    public bool Animated { get; set; }

    /// <summary>Audio: voice note when true.</summary>
    public bool Voice { get; set; }

    public string? Caption { get; set; }
}

