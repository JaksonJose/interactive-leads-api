using InteractiveLeads.Application.Feature.Chat.Messages.Outbound;

namespace InteractiveLeads.Application.Interfaces;

public interface IOutboundMessagePublisher
{
    Task PublishAsync(OutboundMessageContract contract, CancellationToken cancellationToken);
}
