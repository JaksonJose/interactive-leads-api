using System.Text.Json;
using InteractiveLeads.Application.Feature.Inbound.Messages;

namespace InteractiveLeads.Application.Messaging.Contracts;

/// <summary>
/// Inbound message from RabbitMQ. The published JSON has provider/eventType/identifications/payload at root
/// (no wrapper), matching this type; <see cref="NormalizedEvent"/> maps to <see cref="NormalizedInboundEvent"/> for the handler.
/// </summary>
public sealed class InboundIntegrationEvent
{
    public string Provider { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public InboundIdentifications Identifications { get; set; } = new();
    public JsonElement Payload { get; set; }

    /// <summary>Normalized event for <see cref="ProcessInboundEventCommand"/>.</summary>
    public NormalizedInboundEvent NormalizedEvent => new()
    {
        Provider = Provider,
        EventType = EventType,
        Identifications = Identifications,
        Payload = Payload
    };
}
