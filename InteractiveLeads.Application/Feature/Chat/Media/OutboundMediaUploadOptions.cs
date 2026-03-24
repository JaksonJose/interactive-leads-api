namespace InteractiveLeads.Application.Feature.Chat.Media;

/// <summary>Validation and limits for CRM outbound media uploads (chat compose).</summary>
public sealed class OutboundMediaUploadOptions
{
    public const string SectionName = "OutboundMediaUpload";

    /// <summary>
    /// First path segment for S3 keys, aligned with inbound <c>MediaProcessing:FinalPrefix</c> (default <c>whatsapp</c>).
    /// Full key: <c>{StorageRootPrefix}/{tenantId}/{images|documents|audios}/{unique}_{fileName}</c>.
    /// </summary>
    public string StorageRootPrefix { get; set; } = "whatsapp";

    public long MaxImageBytes { get; set; } = 16 * 1024 * 1024;
    public long MaxDocumentBytes { get; set; } = 100 * 1024 * 1024;
    public long MaxAudioBytes { get; set; } = 16 * 1024 * 1024;

    public string[] AllowedImageMimeTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public string[] AllowedDocumentMimeTypes { get; set; } =
    [
        "application/pdf",
        "text/plain",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/csv"
    ];

    public string[] AllowedAudioMimeTypes { get; set; } =
    [
        "audio/ogg",
        "audio/mpeg",
        "audio/mp4",
        "audio/aac",
        "audio/webm",
        "audio/wav",
        "audio/x-wav"
    ];
}
