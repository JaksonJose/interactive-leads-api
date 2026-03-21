using InteractiveLeads.Application.Feature.Chat.Messages.Outbound;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Messaging.Contracts;
using MassTransit;

namespace InteractiveLeads.Infrastructure.Messaging;

/// <summary>
/// Publishes outbound payloads to the configured exchange (fanout → quorum queue per MassTransit topology).
/// </summary>
public sealed class RabbitMqOutboundMessagePublisher(IPublishEndpoint publishEndpoint) : IOutboundMessagePublisher
{
    public Task PublishAsync(OutboundMessageContract contract, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(new OutboundMessageDispatch(contract), cancellationToken);
}
