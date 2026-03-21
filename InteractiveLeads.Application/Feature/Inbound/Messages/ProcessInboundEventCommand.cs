using InteractiveLeads.Application.Feature.Inbound;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Inbound.Messages;

public sealed class ProcessInboundEventCommand : IRequest<IResponse>
{
    public NormalizedInboundEvent Event { get; set; } = new();

    /// <summary>
    /// When true (RabbitMQ consumer), transient failures throw <see cref="InboundTransientException"/> instead of returning TransientRetry.
    /// </summary>
    public bool ReliableMessaging { get; init; }
}
