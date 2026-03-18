using System.Text.Json;

namespace InteractiveLeads.Application.Feature.Webhooks.Messages;

public sealed class WebhookEventRequest
{
    public string Provider { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public WebhookIdentifications Identifications { get; set; } = new();

    /// <summary>
    /// Normalized event payload (message/status/reaction). For now only message is processed.
    /// </summary>
    public JsonElement Payload { get; set; }
}

public sealed class WebhookIdentifications
{
    public string ExternalIdentifier { get; set; } = string.Empty;

    public WebhookContactIdentification Contact { get; set; } = new();
}

public sealed class WebhookContactIdentification
{
    public string Name { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
}

