namespace InteractiveLeads.Application.Feature.Inbound;

/// <summary>
/// Result of processing a normalized inbound channel event (queue or future HTTP).
/// </summary>
public enum InboundProcessingOutcome
{
    Unknown = 0,

    /// <summary>Message stored (new row).</summary>
    Persisted,

    /// <summary>Idempotent skip — external message id already present.</summary>
    DuplicateIgnored,

    /// <summary>Invalid or unsupported payload; broker should ACK without retry.</summary>
    PermanentRejected,

    /// <summary>Transient condition; only returned when <see cref="Messages.ProcessInboundEventCommand.ReliableMessaging"/> is false.</summary>
    TransientRetry
}
