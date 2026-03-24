namespace InteractiveLeads.Application.Feature.Inbound.Media;

public sealed class ProcessMediaRequest
{
    public string TenantId { get; set; } = string.Empty;
    public string MediaUrl { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public string? OriginalFileName { get; set; }
    public string? ExternalMessageId { get; set; }
    public string? IntegrationExternalIdentifier { get; set; }
}

public sealed class MediaVariant
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

public sealed class MediaProcessingResultDto
{
    public string Type { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public string? OptimizedUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? DurationMs { get; set; }
    public string? ContentHash { get; set; }
    public List<MediaVariant> Variants { get; set; } = [];
}

public sealed class MediaObjectDescriptor
{
    public string ObjectKey { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}
