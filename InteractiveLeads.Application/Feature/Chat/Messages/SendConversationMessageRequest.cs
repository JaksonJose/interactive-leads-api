namespace InteractiveLeads.Application.Feature.Chat.Messages;

public sealed class SendConversationMessageRequest
{
    public string Content { get; set; } = string.Empty;

    /// <summary>Optional UNIX seconds for message event time when sending from a client clock.</summary>
    public long? ClientTimestamp { get; set; }

    public string? ExternalMessageId { get; set; }
    public string Type { get; set; } = "text";
    /// <summary>Raw URL sent to the provider (RabbitMQ). For outbound images, use the upload response <c>url</c> (unprocessed file).</summary>
    public string? MediaUrl { get; set; }
    public string? Caption { get; set; }
    public string? MimeType { get; set; }
    public string? FileName { get; set; }
    /// <summary>CRM/Web UI: optimized WebP URL from upload when present; stored on message media instead of <see cref="MediaUrl"/>.</summary>
    public string? MediaOptimizedUrl { get; set; }
    /// <summary>For audio: MIME of <see cref="MediaOptimizedUrl"/> (e.g. <c>audio/mp4</c> for M4A CRM asset).</summary>
    public string? MediaOptimizedMimeType { get; set; }
    /// <summary>For audio: file name of the optimized asset (e.g. <c>.m4a</c>).</summary>
    public string? MediaOptimizedFileName { get; set; }
    /// <summary>CRM / UI: thumbnail WebP URL from the pipeline.</summary>
    public string? MediaThumbnailUrl { get; set; }
    /// <summary>WhatsApp voice note when true; file audio when false.</summary>
    public bool? Voice { get; set; }
    public string? ReactionEmoji { get; set; }
    public Guid? ReactionMessageId { get; set; }
    public Guid? ReplyToMessageId { get; set; }

    /// <summary>WhatsApp template (CRM) id to send when <see cref="Type"/> is <c>template</c>.</summary>
    public Guid? TemplateId { get; set; }

    /// <summary>Template variables for BODY, in order {{1}}, {{2}}, ...</summary>
    public string[]? TemplateBodyParameters { get; set; }

    /// <summary>Optional single variable for HEADER when the template uses a text header.</summary>
    public string? TemplateHeaderParameter { get; set; }
}

