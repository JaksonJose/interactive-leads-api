using System.Text.Json;

namespace InteractiveLeads.Application.Feature.Inbound.Messages;

/// <summary>
/// Normalized provider event (WhatsApp/Instagram, etc.) — same shape as JSON published to the inbound queue or produced by bridges.
/// </summary>
public sealed class NormalizedInboundEvent
{
    public string Provider { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public InboundIdentifications Identifications { get; set; } = new();

    /// <summary>
    /// Provider-specific payload (message, status, etc.).
    /// </summary>
    public JsonElement Payload { get; set; }
}

public sealed class InboundIdentifications
{
    public string ExternalIdentifier { get; set; } = string.Empty;

    public InboundContactInfo Contact { get; set; } = new();
}

public sealed class InboundContactInfo
{
    public string Name { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
}
