using InteractiveLeads.Application.Responses;

namespace InteractiveLeads.Application.Feature.Chat.Messages.Outbound;

/// <summary>Result of handing an outbound message to HTTP or a message broker.</summary>
public sealed class OutboundDispatchOutcome
{
    public required BaseResponse Response { get; init; }

    /// <summary>
    /// When true and <see cref="Response"/> has no errors, the persisted message is updated to <see cref="Domain.Enums.MessageStatus.Sent"/>.
    /// False when dispatch only enqueues (broker): status stays <see cref="Domain.Enums.MessageStatus.Pending"/> until inbound ack/status.
    /// </summary>
    public bool AdvanceToSentOnSuccess { get; init; } = true;
}
