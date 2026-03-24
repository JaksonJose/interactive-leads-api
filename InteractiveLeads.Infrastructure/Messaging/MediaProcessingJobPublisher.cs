using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Messaging.Contracts;
using MassTransit;

namespace InteractiveLeads.Infrastructure.Messaging;

public sealed class MediaProcessingJobPublisher(IPublishEndpoint publishEndpoint) : IMediaProcessingJobPublisher
{
    public Task PublishAsync(MediaProcessingRequested job, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(job, cancellationToken);
}
