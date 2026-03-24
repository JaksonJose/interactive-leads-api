namespace InteractiveLeads.Application.Feature.Chat.Media;

public sealed class ConversationMediaUploadResultDto
{
    /// <summary>Public URL of the file as stored for channel delivery (raw bytes, no processing).</summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>S3 object key for <see cref="Url"/>.</summary>
    public string ObjectKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    /// <summary>Content-Type of <see cref="Url"/> (same as upload).</summary>
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    /// <summary>Storage segment: image, document, or audio.</summary>
    public string MediaType { get; set; } = string.Empty;

    /// <summary>Same as <see cref="Url"/> for images (raw); kept for older clients.</summary>
    public string? OriginalUrl { get; set; }
    /// <summary>CRM / UI: WebP optimized variant when processing succeeded.</summary>
    public string? OptimizedUrl { get; set; }
    /// <summary>For audio: MIME of <see cref="OptimizedUrl"/> (e.g. <c>audio/mp4</c> for M4A). Null when same asset as <see cref="Url"/>.</summary>
    public string? OptimizedMimeType { get; set; }
    /// <summary>For audio: file name of the optimized asset (e.g. <c>.m4a</c>).</summary>
    public string? OptimizedFileName { get; set; }
    public string? ThumbnailUrl { get; set; }
}
