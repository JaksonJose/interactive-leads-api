namespace InteractiveLeads.Application.Feature.Chat.Media;

/// <summary>Validation and limits for CRM outbound media uploads (chat compose).</summary>
public sealed class OutboundMediaUploadOptions
{
    public const string SectionName = "OutboundMediaUpload";

    /// <summary>
    /// First path segment for S3 keys, aligned with inbound <c>MediaProcessing:FinalPrefix</c> (default <c>whatsapp</c>).
    /// Full key: <c>{StorageRootPrefix}/{tenantId}/{images|documents|audios|videos}/{unique}_{fileName}</c>.
    /// </summary>
    public string StorageRootPrefix { get; set; } = "whatsapp";

    public long MaxImageBytes { get; set; } = 16 * 1024 * 1024;
    public long MaxDocumentBytes { get; set; } = 100 * 1024 * 1024;
    public long MaxAudioBytes { get; set; } = 16 * 1024 * 1024;
    public long MaxVideoBytes { get; set; } = 100 * 1024 * 1024;

    /// <summary>FFmpeg binary for converting WebM, WAV, M4A/MP4 audio to Ogg Opus (WhatsApp-friendly). Default <c>ffmpeg</c> (PATH).</summary>
    public string FfmpegExecutable { get; set; } = "ffmpeg";

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

    /// <summary>WhatsApp-friendly audio sent as-is. M4A/MP4 (AAC), WebM and WAV are transcoded to Ogg Opus before upload for delivery.</summary>
    public string[] AllowedAudioMimeTypes { get; set; } =
    [
        "audio/ogg",
        "audio/mpeg",
        "audio/aac",
        "audio/amr"
    ];

    public string[] AllowedVideoMimeTypes { get; set; } =
    [
        "video/mp4",
        "video/3gpp",
        "video/quicktime",
        "video/webm"
    ];
}
