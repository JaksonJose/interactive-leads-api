namespace InteractiveLeads.Application.Feature.Inbound;

/// <summary>
/// Thrown when processing must be retried (technical failure or eventual-consistency business case).
/// With reliable queue delivery, the consumer rethrows so the queue worker retries/redelivers.
/// </summary>
public sealed class InboundTransientException : Exception
{
    public string ReasonCode { get; }

    public InboundTransientException(string reasonCode, string? message = null, Exception? innerException = null)
        : base(message ?? reasonCode, innerException)
    {
        ReasonCode = reasonCode;
    }
}
