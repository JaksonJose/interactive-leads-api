namespace InteractiveLeads.Infrastructure.Configuration;

/// <summary>
/// RabbitMQ / MassTransit connection. Prefer environment variables in deployment (see remarks).
/// </summary>
/// <remarks>
/// Environment variable names (double underscore in .NET):
/// <list type="bullet">
/// <item><description>RabbitMq__Host</description></item>
/// <item><description>RabbitMq__Port</description></item>
/// <item><description>RabbitMq__VirtualHost</description></item>
/// <item><description>RabbitMq__Username</description></item>
/// <item><description>RabbitMq__Password</description></item>
/// <item><description>RabbitMq__Enabled</description> — set false to disable MassTransit (no inbound consumer; outbound uses HTTP only).</item>
/// <item><description>Integration__MessageSender__UseHttpFallback</description> — when Rabbit is enabled, use HTTP outbound instead of the broker queue.</item>
/// <item><description>RabbitMq__InboundQueueName</description> — optional override for the inbound queue (default <c>interactive-inbound-events</c>).</item>
/// <item><description>RabbitMq__OutboundQueueName</description> — optional override for the outbound queue (default <c>interactive-outbound-send</c>).</item>
/// <item><description>RabbitMq__OutboundExchangeName</description> — exchange used when publishing outbound messages (default <c>interactive-outbound</c>).</item>
/// <item><description>RabbitMq__UseQuorumQueues</description> — when true (default), inbound receive endpoint and outbound queue binding use quorum queues.</item>
/// <item><description>RabbitMq__UnprocessedQueueName</description> — optional queue for permanently rejected inbound copies (default <c>chat.unprocessed</c>).</item>
/// <item><description>RabbitMq__ForwardPermanentRejectionsToUnprocessedQueue</description> — when true, send a copy to the unprocessed queue on permanent reject.</item>
/// </list>
/// </remarks>
public sealed class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    /// <summary>When false, MassTransit is not registered (inbound queue is not consumed).</summary>
    public bool Enabled { get; set; } = true;

    public string Host { get; set; } = "localhost";

    public ushort Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    public string Username { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    /// <summary>Queue consumed by this API for inbound messages published by external integrations.</summary>
    public string InboundQueueName { get; set; } = "interactive-inbound-events";

    /// <summary>Queue where this API sends outbound messages for external channel workers.</summary>
    public string OutboundQueueName { get; set; } = "interactive-outbound-send";

    /// <summary>Exchange outbound messages are published to; bound to the outbound queue by MassTransit at startup.</summary>
    public string OutboundExchangeName { get; set; } = "interactive-outbound";

    /// <summary>When true, use RabbitMQ quorum queues for inbound consumer and outbound queue declaration.</summary>
    public bool UseQuorumQueues { get; set; } = true;

    /// <summary>Optional queue for operator review of permanently rejected inbound events.</summary>
    public string UnprocessedQueueName { get; set; } = "chat.unprocessed";
    public string MediaProcessingQueueName { get; set; } = "media.processing";

    /// <summary>When true, publish a copy of the event to <see cref="UnprocessedQueueName"/> on permanent rejection.</summary>
    public bool ForwardPermanentRejectionsToUnprocessedQueue { get; set; }
}
