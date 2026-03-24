using InteractiveLeads.Application.Feature.Inbound.Media;

namespace InteractiveLeads.Application.Interfaces;

public interface IMediaProcessor
{
    Task<MediaProcessingResultDto> ProcessAsync(ProcessMediaRequest request, CancellationToken cancellationToken);
}
