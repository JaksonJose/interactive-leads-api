namespace InteractiveLeads.Application.Feature.Inbound.Messages;

/// <summary>WhatsApp (and similar) delivery/read receipts published with <c>eventType: "status"</c>.</summary>
public sealed class InboundStatusPayload
{
    /// <summary>Provider message id (e.g. WhatsApp <c>wamid.*</c>), matches <see cref="InteractiveLeads.Domain.Entities.Message.ExternalMessageId"/>.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Provider status: <c>sent</c>, <c>delivered</c>, <c>read</c>, <c>failed</c>.</summary>
    public string Status { get; set; } = string.Empty;

    public long Timestamp { get; set; }

    /// <summary>Optional; status events still target the outbound row identified by <see cref="MessageId"/>.</summary>
    public string? Direction { get; set; }
}
