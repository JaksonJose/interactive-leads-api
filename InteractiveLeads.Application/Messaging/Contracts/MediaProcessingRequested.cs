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
    public DateTimeOffset CreatedAt { get; set; }
    public string? Caption { get; set; }
    public string? ExternalMessageId { get; set; }
}
