using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Messaging.Contracts;
using Microsoft.Extensions.Logging;

namespace InteractiveLeads.Infrastructure.Messaging;

public sealed class NoopMediaProcessingJobPublisher(ILogger<NoopMediaProcessingJobPublisher> logger) : IMediaProcessingJobPublisher
{
    public Task PublishAsync(MediaProcessingRequested job, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Media processing job not published because RabbitMQ is disabled. messageId {MessageId} tenantId {TenantId}",
            job.MessageId,
            job.TenantId);
        return Task.CompletedTask;
    }
}
