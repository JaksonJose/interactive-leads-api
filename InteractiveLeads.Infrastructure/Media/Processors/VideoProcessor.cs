using InteractiveLeads.Application.Feature.Inbound.Media;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Infrastructure.Configuration;
using InteractiveLeads.Infrastructure.Media;
using Microsoft.Extensions.Options;

namespace InteractiveLeads.Infrastructure.Media.Processors;

public sealed class VideoProcessor(
    IMediaStorageGateway storageGateway,
    IOptions<MediaProcessingOptions> options) : IVideoProcessor
{
    private readonly MediaProcessingOptions _options = options.Value;

    public async Task<MediaProcessingResultDto> ProcessAsync(
        string localInputPath,
        ProcessMediaRequest request,
        string contentHash,
        CancellationToken cancellationToken)
    {
        var originalSuffix = MediaOriginalKeyExtensions.GetOriginalExtension(request);
        var baseKey = $"{_options.FinalPrefix.TrimEnd('/')}/{request.TenantId.Trim()}/videos/{contentHash}";

        await using var stream = File.OpenRead(localInputPath);
        var originalObj = await storageGateway.UploadAsync(
            $"{baseKey}/original{originalSuffix}",
            stream,
            request.MimeType ?? "video/mp4",
            cancellationToken);

        return new MediaProcessingResultDto
        {
            Type = "video",
            OriginalUrl = originalObj.PublicUrl,
            OptimizedUrl = originalObj.PublicUrl,
            ThumbnailUrl = null,
            ContentHash = contentHash,
            Variants =
            [
                new() { Name = "original", Url = originalObj.PublicUrl, ContentType = originalObj.ContentType }
            ]
        };
    }
}
