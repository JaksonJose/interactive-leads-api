using InteractiveLeads.Application.Feature.Inbound.Media;

namespace InteractiveLeads.Application.Interfaces;

public interface IImageProcessor
{
    Task<MediaProcessingResultDto> ProcessAsync(Stream input, ProcessMediaRequest request, string contentHash, CancellationToken cancellationToken);
}
