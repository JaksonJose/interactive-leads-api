using System.Text.Json;
using InteractiveLeads.Application.Feature.Webhooks.Messages;

namespace InteractiveLeads.Application.Messaging.Contracts;

/// <summary>
/// Inbound message from RabbitMQ. The published JSON has provider/eventType/identifications/payload at root
/// (no "event" wrapper), so this type matches that structure and exposes Event for the handler.
/// </summary>
public sealed class InboundIntegrationEvent
{
    public string Provider { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public WebhookIdentifications Identifications { get; set; } = new();
    public JsonElement Payload { get; set; }

    /// <summary>WebhookEventRequest built from root-level properties for ProcessWebhookEventCommand.</summary>
    public WebhookEventRequest Event => new()
    {
        Provider = Provider,
        EventType = EventType,
        Identifications = Identifications,
        Payload = Payload
    };
}
