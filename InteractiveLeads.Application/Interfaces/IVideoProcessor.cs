using InteractiveLeads.Application.Feature.Inbound.Media;

namespace InteractiveLeads.Application.Interfaces;

public interface IVideoProcessor
{
    Task<MediaProcessingResultDto> ProcessAsync(string localInputPath, ProcessMediaRequest request, string contentHash, CancellationToken cancellationToken);
}
