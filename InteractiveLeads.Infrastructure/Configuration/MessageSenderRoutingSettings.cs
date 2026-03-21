namespace InteractiveLeads.Infrastructure.Configuration;

/// <summary>
/// Outbound message transport when RabbitMQ is enabled.
/// </summary>
public sealed class MessageSenderRoutingSettings
{
    public const string SectionPath = "Integration:MessageSender";

    /// <summary>When true, outbound uses HTTP (Integration:MessageSender) instead of the RabbitMQ outbound queue.</summary>
    public bool UseHttpFallback { get; set; }
}
