using InteractiveLeads.Application.Feature.Chat.Messages.Outbound;

namespace InteractiveLeads.Application.Interfaces;

/// <summary>
/// Delivers outbound messages to the configured channel (HTTP or message broker). The API does not know the specific external system.
/// </summary>
public interface IOutboundMessageDispatcher
{
    /// <returns>Transport outcome; see <see cref="OutboundDispatchOutcome.AdvanceToSentOnSuccess"/> for when the row becomes <c>Sent</c>.</returns>
    Task<OutboundDispatchOutcome> SendMessageAsync(OutboundMessageContract payload, CancellationToken cancellationToken);
}
