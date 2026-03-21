namespace InteractiveLeads.Application.Messaging.Contracts;

/// <summary>
/// Optional copy of a permanently rejected inbound event for manual review (e.g. queue chat.unprocessed).
/// </summary>
public sealed class InboundUnprocessedDispatch
{
    public string ReasonCode { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string? ExternalIdentifier { get; set; }

    public string? MessageId { get; set; }

    /// <summary>Original JSON body as string for operators.</summary>
    public string? RawEventJson { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
