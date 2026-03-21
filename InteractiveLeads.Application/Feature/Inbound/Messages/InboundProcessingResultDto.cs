using InteractiveLeads.Application.Feature.Inbound;

namespace InteractiveLeads.Application.Feature.Inbound.Messages;

public sealed class InboundProcessingResultDto
{
    /// <summary>True when the message was persisted or handled idempotently (duplicate).</summary>
    public bool Processed { get; set; }

    public string? Reason { get; set; }

    public InboundProcessingOutcome Outcome { get; set; } = InboundProcessingOutcome.Unknown;
}
