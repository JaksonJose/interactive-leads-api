namespace InteractiveLeads.Infrastructure.Configuration;

public sealed class MediaProcessingOptions
{
    public const string SectionName = "MediaProcessing";

    public string BucketName { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
    public string TempPrefix { get; set; } = "whatsapp/temp";
    public string FinalPrefix { get; set; } = "whatsapp";
    public int MaxImageWidth { get; set; } = 1280;
    public int ThumbnailWidth { get; set; } = 300;

    // AWS settings for development/local runs.
    // Prefer environment variables or IAM role in production.
    public string AwsRegion { get; set; } = "sa-east-1";
    public string? AwsAccessKeyId { get; set; }
    public string? AwsSecretAccessKey { get; set; }
}
