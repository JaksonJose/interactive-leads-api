using InteractiveLeads.Application.Feature.Inbound.Media;

namespace InteractiveLeads.Application.Interfaces;

public interface IAudioProcessor
{
    Task<MediaProcessingResultDto> ProcessAsync(string localInputPath, ProcessMediaRequest request, string contentHash, CancellationToken cancellationToken);
}
