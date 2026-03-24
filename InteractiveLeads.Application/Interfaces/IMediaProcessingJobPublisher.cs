using InteractiveLeads.Application.Messaging.Contracts;

namespace InteractiveLeads.Application.Interfaces;

public interface IMediaProcessingJobPublisher
{
    Task PublishAsync(MediaProcessingRequested job, CancellationToken cancellationToken);
}
