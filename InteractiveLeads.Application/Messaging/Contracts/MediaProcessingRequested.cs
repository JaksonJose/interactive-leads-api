namespace InteractiveLeads.Application.Messaging.Contracts;

public sealed class MediaProcessingRequested
{
    public string TenantId { get; set; } = string.Empty;
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid IntegrationId { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public string TempUrl { get; set; } = string.Empty;
    public string? MimeType { get; set; }

    /// <summary>Logical message instant (channel / payload time).</summary>
    public DateTimeOffset MessageDate { get; set; }
    public string? Caption { get; set; }
    public string? ExternalMessageId { get; set; }
    public string? OriginalFileName { get; set; }

    public bool Animated { get; set; }

    public bool Voice { get; set; }
}
