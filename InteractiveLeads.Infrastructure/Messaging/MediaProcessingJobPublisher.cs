using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Messaging.Contracts;
using Rebus.Bus;

namespace InteractiveLeads.Infrastructure.Messaging;

public sealed class MediaProcessingJobPublisher(IBus bus) : IMediaProcessingJobPublisher
{
    public Task PublishAsync(MediaProcessingRequested job, CancellationToken cancellationToken) =>
        bus.Send(job);
}
