using InteractiveLeads.Application.Feature.Inbound.Media;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace InteractiveLeads.Infrastructure.Media.Processors;

public sealed class ImageProcessor(
    IMediaStorageGateway storageGateway,
    IOptions<MediaProcessingOptions> options) : IImageProcessor
{
    private readonly MediaProcessingOptions _options = options.Value;

    public async Task<MediaProcessingResultDto> ProcessAsync(
        Stream input,
        ProcessMediaRequest request,
        string contentHash,
        CancellationToken cancellationToken)
    {
        using var image = await Image.LoadAsync(input, cancellationToken);
        var width = image.Width;
        var height = image.Height;

        var baseKey = $"{_options.FinalPrefix.TrimEnd('/')}/{request.TenantId.Trim()}/images/{contentHash}";

        var original = string.Equals(request.MediaType, "sticker", StringComparison.OrdinalIgnoreCase)
            ? await UploadStickerOriginalWebpAsync(image, $"{baseKey}/original.webp", cancellationToken)
            : await UploadPhotoOriginalAsync(image, $"{baseKey}/original", request, cancellationToken);
        var optimized = await UploadResizedAsync(image, $"{baseKey}/optimized.webp", _options.MaxImageWidth, cancellationToken);
        var thumb = await UploadResizedAsync(image, $"{baseKey}/thumbnail.webp", _options.ThumbnailWidth, cancellationToken);

        return new MediaProcessingResultDto
        {
            Type = "image",
            OriginalUrl = original.PublicUrl,
            OptimizedUrl = optimized.PublicUrl,
            ThumbnailUrl = thumb.PublicUrl,
            Width = width,
            Height = height,
            ContentHash = contentHash,
            Variants =
            [
                new() { Name = "original", Url = original.PublicUrl, ContentType = original.ContentType },
                new() { Name = "optimized", Url = optimized.PublicUrl, ContentType = optimized.ContentType },
                new() { Name = "thumbnail", Url = thumb.PublicUrl, ContentType = thumb.ContentType }
            ]
        };
    }

    /// <summary>
    /// Raster photos: preserve JPEG/PNG-style originals for channel upload (e.g. WhatsApp); WebP is reserved for optimized/thumbnail.
    /// </summary>
    private async Task<MediaObjectDescriptor> UploadPhotoOriginalAsync(
        Image image,
        string keyWithoutExtension,
        ProcessMediaRequest request,
        CancellationToken cancellationToken)
    {
        var usePng = ShouldEncodeOriginalAsPng(request, image);
        var key = usePng ? $"{keyWithoutExtension}.png" : $"{keyWithoutExtension}.jpg";
        var contentType = usePng ? "image/png" : "image/jpeg";

        using var stream = new MemoryStream();
        if (usePng)
            await image.SaveAsPngAsync(stream, cancellationToken);
        else
            await image.SaveAsJpegAsync(stream, new JpegEncoder { Quality = 92 }, cancellationToken);

        stream.Position = 0;
        return await storageGateway.UploadAsync(key, stream, contentType, cancellationToken);
    }

    private static bool ShouldEncodeOriginalAsPng(ProcessMediaRequest request, Image image)
    {
        var mime = request.MimeType ?? string.Empty;
        if (mime.Contains("png", StringComparison.OrdinalIgnoreCase))
            return true;

        var decoded = image.Metadata.DecodedImageFormat?.Name;
        if (decoded != null && decoded.Equals("PNG", StringComparison.OrdinalIgnoreCase))
            return true;

        if (image.PixelType.AlphaRepresentation != PixelAlphaRepresentation.None)
            return true;

        return false;
    }

    private async Task<MediaObjectDescriptor> UploadStickerOriginalWebpAsync(
        Image image,
        string key,
        CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        await image.SaveAsWebpAsync(stream, new WebpEncoder { Quality = 85 }, cancellationToken);
        stream.Position = 0;
        return await storageGateway.UploadAsync(key, stream, "image/webp", cancellationToken);
    }

    private async Task<MediaObjectDescriptor> UploadResizedAsync(
        Image source,
        string key,
        int maxWidth,
        CancellationToken cancellationToken)
    {
        using var clone = source.Clone(ctx =>
        {
            if (source.Width > maxWidth)
                ctx.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(maxWidth, maxWidth) });
        });
        using var stream = new MemoryStream();
        await clone.SaveAsWebpAsync(stream, new WebpEncoder { Quality = 75 }, cancellationToken);
        stream.Position = 0;
        return await storageGateway.UploadAsync(key, stream, "image/webp", cancellationToken);
    }
}
