using InteractiveLeads.Application.Feature.Chat.Media;
using InteractiveLeads.Application.Feature.Inbound.Media;

namespace InteractiveLeads.Application.Interfaces;

public interface IImageProcessor
{
    Task<MediaProcessingResultDto> ProcessAsync(Stream input, ProcessMediaRequest request, string contentHash, CancellationToken cancellationToken);

    /// <summary>Decode raster image and encode as WhatsApp-friendly JPEG or PNG (alpha → PNG).</summary>
    Task<OutboundWhatsAppImageEncodingResult> EncodeForWhatsAppDeliveryAsync(
        Stream source,
        string sourceMimeType,
        CancellationToken cancellationToken);
}
