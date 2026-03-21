using InteractiveLeads.Application.Feature.Chat.Messages.Outbound;
using InteractiveLeads.Application.Responses;

namespace InteractiveLeads.Application.Interfaces;

/// <summary>
/// Delivers outbound messages to the configured channel (HTTP or message broker). The API does not know the specific external system.
/// </summary>
public interface IOutboundMessageDispatcher
{
    Task<BaseResponse> SendMessageAsync(OutboundMessageContract payload, CancellationToken cancellationToken);
}
