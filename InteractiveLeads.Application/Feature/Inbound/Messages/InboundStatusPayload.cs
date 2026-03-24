namespace InteractiveLeads.Application.Feature.Inbound.Messages;

/// <summary>WhatsApp (and similar) delivery/read receipts published with <c>eventType: "status"</c>.</summary>
public sealed class InboundStatusPayload
{
    /// <summary>Provider message id (e.g. WhatsApp <c>wamid.*</c>), matches <see cref="InteractiveLeads.Domain.Entities.Message.ExternalMessageId"/>.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Alternate JSON key used by some bridges for the same value as <see cref="MessageId"/>.</summary>
    public string? Id { get; set; }

    /// <summary>
    /// Internal message id (<see cref="InteractiveLeads.Domain.Entities.Message.Id"/>), same as outbound <c>clientMessageId</c>.
    /// Use when the provider never assigned an id (e.g. send failed before ack).
    /// </summary>
    public string? ClientMessageId { get; set; }

    /// <summary>Provider status: <c>sent</c>, <c>delivered</c>, <c>read</c>, <c>failed</c>.</summary>
    public string Status { get; set; } = string.Empty;

    public long Timestamp { get; set; }

    /// <summary>Optional; status events still target the outbound row identified by <see cref="MessageId"/>.</summary>
    public string? Direction { get; set; }
}
