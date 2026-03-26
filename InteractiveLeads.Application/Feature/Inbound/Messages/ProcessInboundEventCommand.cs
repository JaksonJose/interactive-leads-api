using InteractiveLeads.Application.Feature.Inbound;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace InteractiveLeads.Application.Feature.Inbound.Messages;

public sealed class ProcessInboundEventCommand : IApplicationRequest<IResponse>
{
    public NormalizedInboundEvent Event { get; set; } = new();

    /// <summary>
    /// When true (RabbitMQ consumer), the handler avoids returning <see cref="InboundProcessingOutcome.TransientRetry"/> for known cases; unhandled exceptions still trigger retries.
    /// </summary>
    public bool ReliableMessaging { get; init; }
}

