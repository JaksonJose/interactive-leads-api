namespace InteractiveLeads.Application.Feature.Chat.Media;

public sealed class ConversationMediaUploadResultDto
{
    public string Url { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    /// <summary>Storage segment: image, document, or audio.</summary>
    public string MediaType { get; set; } = string.Empty;

    /// <summary>Populated for images after WebP pipeline (same layout as inbound).</summary>
    public string? OriginalUrl { get; set; }
    public string? OptimizedUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
}
