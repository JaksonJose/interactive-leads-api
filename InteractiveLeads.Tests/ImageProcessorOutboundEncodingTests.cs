using InteractiveLeads.Application.Feature.Inbound.Media;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Infrastructure.Configuration;
using InteractiveLeads.Infrastructure.Media.Processors;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace InteractiveLeads.Tests;

public sealed class ImageProcessorOutboundEncodingTests
{
    private sealed class NopStorage : IMediaStorageGateway
    {
        public Task<Stream> DownloadAsync(string sourceUrlOrKey, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MediaObjectDescriptor> UploadAsync(
            string objectKey,
            Stream content,
            string contentType,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    [Fact]
    public async Task EncodeForWhatsAppDeliveryAsync_jpeg_input_returns_jpeg_or_png_stream()
    {
        using var image = new Image<Rgb24>(8, 8, Color.Blue);
        await using var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms);
        ms.Position = 0;

        var processor = new ImageProcessor(new NopStorage(), Options.Create(new MediaProcessingOptions { FinalPrefix = "w" }));
        await using var result = await processor.EncodeForWhatsAppDeliveryAsync(ms, "image/jpeg", CancellationToken.None);

        Assert.True(result.Stream.Length > 0);
        if (result.ContentType == "image/jpeg")
            Assert.Equal(".jpg", result.FileExtension);
        else
        {
            Assert.Equal("image/png", result.ContentType);
            Assert.Equal(".png", result.FileExtension);
        }
    }

    [Fact]
    public async Task EncodeForWhatsAppDeliveryAsync_rgba_png_input_returns_png_stream()
    {
        using var image = new Image<Rgba32>(4, 4, new Rgba32(255, 0, 0, 128));
        await using var ms = new MemoryStream();
        await image.SaveAsPngAsync(ms);
        ms.Position = 0;

        var processor = new ImageProcessor(new NopStorage(), Options.Create(new MediaProcessingOptions { FinalPrefix = "w" }));
        await using var result = await processor.EncodeForWhatsAppDeliveryAsync(ms, "image/png", CancellationToken.None);

        Assert.Equal("image/png", result.ContentType);
        Assert.Equal(".png", result.FileExtension);
    }
}
