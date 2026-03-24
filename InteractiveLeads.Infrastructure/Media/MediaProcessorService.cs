using InteractiveLeads.Application.Feature.Inbound.Media;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InteractiveLeads.Infrastructure.Media;

public sealed class MediaProcessorService(
    IMediaStorageGateway storageGateway,
    IMediaContentInspector contentInspector,
    IImageProcessor imageProcessor,
    IVideoProcessor videoProcessor,
    IAudioProcessor audioProcessor,
    IOptions<MediaProcessingOptions> options,
    ILogger<MediaProcessorService> logger) : IMediaProcessor
{
    private readonly MediaProcessingOptions _options = options.Value;

    public async Task<MediaProcessingResultDto> ProcessAsync(ProcessMediaRequest request, CancellationToken cancellationToken)
    {
        await using var source = await storageGateway.DownloadAsync(request.MediaUrl, cancellationToken);
        var hash = await contentInspector.ComputeSha256Async(source, cancellationToken);

        var mediaType = request.MediaType.Trim().ToLowerInvariant();
        logger.LogInformation("Processing inbound media type {MediaType} hash {Hash}", mediaType, hash);

        var existing = await TryResolveExistingAsync(mediaType, request, hash, cancellationToken);
        if (existing is not null)
            return existing;

        var result = mediaType switch
        {
            "image" or "sticker" => await imageProcessor.ProcessAsync(source, request, hash, cancellationToken),
            "video" => await ProcessWithTempFileAsync(source, async path => await videoProcessor.ProcessAsync(path, request, hash, cancellationToken), cancellationToken),
            "audio" => await ProcessWithTempFileAsync(source, async path => await audioProcessor.ProcessAsync(path, request, hash, cancellationToken), cancellationToken),
            "document" => await MoveOnlyAsync(source, request, hash, cancellationToken),
            _ => throw new NotSupportedException($"Unsupported media type: {request.MediaType}")
        };

        await TryDeleteTempObjectAsync(request.MediaUrl, cancellationToken);
        return result;
    }

    private async Task<MediaProcessingResultDto> MoveOnlyAsync(Stream source, ProcessMediaRequest request, string hash, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(request.OriginalFileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".bin";

        var key = $"{_options.FinalPrefix.TrimEnd('/')}/{request.TenantId.Trim()}/documents/{hash}/original{extension}";
        if (await storageGateway.ExistsAsync(key, cancellationToken))
        {
            var existsUrl = BuildPublicUrl(key);
            return new MediaProcessingResultDto
            {
                Type = "document",
                OriginalUrl = existsUrl,
                ContentHash = hash
            };
        }

        var uploaded = await storageGateway.UploadAsync(key, source, request.MimeType ?? "application/octet-stream", cancellationToken);
        return new MediaProcessingResultDto
        {
            Type = "document",
            OriginalUrl = uploaded.PublicUrl,
            ContentHash = hash
        };
    }

    private async Task<MediaProcessingResultDto> ProcessWithTempFileAsync(
        Stream source,
        Func<string, Task<MediaProcessingResultDto>> processAsync,
        CancellationToken cancellationToken)
    {
        var hash = Guid.NewGuid().ToString("N");
        var path = Path.Combine(Path.GetTempPath(), $"{hash}-{Guid.NewGuid():N}.bin");
        try
        {
            await using (var file = File.Create(path))
            {
                if (source.CanSeek)
                    source.Position = 0;
                await source.CopyToAsync(file, cancellationToken);
            }

            return await processAsync(path);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private string BuildPublicUrl(string key)
    {
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}";

        return $"https://{_options.BucketName}.s3.amazonaws.com/{key}";
    }

    private async Task<MediaProcessingResultDto?> TryResolveExistingAsync(
        string mediaType,
        ProcessMediaRequest request,
        string hash,
        CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId.Trim();
        var prefix = _options.FinalPrefix.TrimEnd('/');

        switch (mediaType)
        {
            case "image" or "sticker":
            {
                var optimizedKey = $"{prefix}/{tenantId}/images/{hash}/optimized.webp";
                if (!await storageGateway.ExistsAsync(optimizedKey, cancellationToken))
                    return null;

                var optimizedUrl = BuildPublicUrl(optimizedKey);

                string? originalUrl = null;
                foreach (var name in new[] { "original.jpg", "original.png", "original.webp" })
                {
                    var candidate = $"{prefix}/{tenantId}/images/{hash}/{name}";
                    if (await storageGateway.ExistsAsync(candidate, cancellationToken))
                    {
                        originalUrl = BuildPublicUrl(candidate);
                        break;
                    }
                }

                originalUrl ??= optimizedUrl;

                return new MediaProcessingResultDto
                {
                    Type = mediaType,
                    OriginalUrl = originalUrl,
                    OptimizedUrl = optimizedUrl,
                    ContentHash = hash
                };
            }
            case "video":
                return await TryResolveExistingVideoAsync(prefix, tenantId, hash, request, cancellationToken);
            case "audio":
                return await TryResolveExistingAudioAsync(prefix, tenantId, hash, request, cancellationToken);
            case "document":
            {
                var key = $"{prefix}/{tenantId}/documents/{hash}/original.bin";
                if (!await storageGateway.ExistsAsync(key, cancellationToken))
                    return null;
                var url = BuildPublicUrl(key);
                return new MediaProcessingResultDto
                {
                    Type = mediaType,
                    OriginalUrl = url,
                    ContentHash = hash
                };
            }
            default:
                return null;
        }
    }

    private async Task<MediaProcessingResultDto?> TryResolveExistingVideoAsync(
        string prefix,
        string tenantId,
        string hash,
        ProcessMediaRequest request,
        CancellationToken cancellationToken)
    {
        var baseKey = $"{prefix}/{tenantId}/videos/{hash}";

        var newOriginal = $"{baseKey}/original{MediaOriginalKeyExtensions.GetOriginalExtension(request)}";
        if (await storageGateway.ExistsAsync(newOriginal, cancellationToken))
        {
            var url = BuildPublicUrl(newOriginal);
            return new MediaProcessingResultDto
            {
                Type = "video",
                OriginalUrl = url,
                OptimizedUrl = url,
                ThumbnailUrl = null,
                ContentHash = hash
            };
        }

        var optimizedKey = $"{baseKey}/optimized.mp4";
        if (!await storageGateway.ExistsAsync(optimizedKey, cancellationToken))
            return null;

        string? thumbUrl = null;
        var thumbKey = $"{baseKey}/thumbnail.jpg";
        if (await storageGateway.ExistsAsync(thumbKey, cancellationToken))
            thumbUrl = BuildPublicUrl(thumbKey);

        var legacyOriginalKey = $"{baseKey}/original.mp4";
        var originalUrl = await storageGateway.ExistsAsync(legacyOriginalKey, cancellationToken)
            ? BuildPublicUrl(legacyOriginalKey)
            : BuildPublicUrl(optimizedKey);

        return new MediaProcessingResultDto
        {
            Type = "video",
            OriginalUrl = originalUrl,
            OptimizedUrl = BuildPublicUrl(optimizedKey),
            ThumbnailUrl = thumbUrl,
            ContentHash = hash
        };
    }

    private async Task<MediaProcessingResultDto?> TryResolveExistingAudioAsync(
        string prefix,
        string tenantId,
        string hash,
        ProcessMediaRequest request,
        CancellationToken cancellationToken)
    {
        var baseKey = $"{prefix}/{tenantId}/audios/{hash}";

        var newOriginal = $"{baseKey}/original{MediaOriginalKeyExtensions.GetOriginalExtension(request)}";
        if (await storageGateway.ExistsAsync(newOriginal, cancellationToken))
        {
            var url = BuildPublicUrl(newOriginal);
            return new MediaProcessingResultDto
            {
                Type = "audio",
                OriginalUrl = url,
                OptimizedUrl = url,
                ContentHash = hash
            };
        }

        var legacyProcessed = $"{baseKey}/processed.mp3";
        if (!await storageGateway.ExistsAsync(legacyProcessed, cancellationToken))
            return null;

        var legacyUrl = BuildPublicUrl(legacyProcessed);
        return new MediaProcessingResultDto
        {
            Type = "audio",
            OriginalUrl = legacyUrl,
            OptimizedUrl = legacyUrl,
            ContentHash = hash
        };
    }

    private async Task TryDeleteTempObjectAsync(string mediaUrl, CancellationToken cancellationToken)
    {
        try
        {
            var path = Uri.TryCreate(mediaUrl, UriKind.Absolute, out var uri)
                ? uri.AbsolutePath.TrimStart('/')
                : mediaUrl.TrimStart('/');

            if (path.StartsWith(_options.TempPrefix.Trim('/'), StringComparison.OrdinalIgnoreCase))
                await storageGateway.DeleteAsync(path, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete temp media object {MediaUrl}", mediaUrl);
        }
    }
}
