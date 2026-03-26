using InteractiveLeads.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace InteractiveLeads.Infrastructure.Messaging;

public sealed class RabbitMqTopologyInitializer(
    IRabbitMqConnectionFactoryProvider connectionFactoryProvider,
    IOptions<RabbitMqSettings> options) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;

        await using var connection = await connectionFactoryProvider.Create().CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var quorumArgs = settings.UseQuorumQueues
            ? new Dictionary<string, object?> { ["x-queue-type"] = "quorum" }
            : null;

        // Outbound fanout
        await channel.ExchangeDeclareAsync(
            exchange: settings.OutboundExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: settings.OutboundQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: quorumArgs,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: settings.OutboundQueueName,
            exchange: settings.OutboundExchangeName,
            routingKey: string.Empty,
            cancellationToken: cancellationToken);

        // Inbound raw queue + error queue
        await channel.QueueDeclareAsync(
            queue: settings.InboundQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: quorumArgs,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: $"{settings.InboundQueueName}.error",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: quorumArgs,
            cancellationToken: cancellationToken);

        // Media processing queue + error queue (Rebus input queue)
        await channel.QueueDeclareAsync(
            queue: settings.MediaProcessingQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: quorumArgs,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: $"{settings.MediaProcessingQueueName}.error",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: quorumArgs,
            cancellationToken: cancellationToken);

        // Unprocessed queue (operator review)
        await channel.QueueDeclareAsync(
            queue: settings.UnprocessedQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: quorumArgs,
            cancellationToken: cancellationToken);

        // Delayed redelivery queues for inbound (TTL -> dead-letter back to inbound queue via default exchange).
        await DeclareDelayQueueAsync(channel, settings, "30s", ttlMs: 30_000, quorumArgs, cancellationToken);
        await DeclareDelayQueueAsync(channel, settings, "2m", ttlMs: 120_000, quorumArgs, cancellationToken);
        await DeclareDelayQueueAsync(channel, settings, "10m", ttlMs: 600_000, quorumArgs, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static Task DeclareDelayQueueAsync(
        IChannel channel,
        RabbitMqSettings settings,
        string suffix,
        int ttlMs,
        IDictionary<string, object?>? quorumArgs,
        CancellationToken cancellationToken)
    {
        var delayQueue = $"{settings.InboundQueueName}.delay.{suffix}";

        var args = new Dictionary<string, object?>();
        if (quorumArgs != null)
            foreach (var kvp in quorumArgs) args[kvp.Key] = kvp.Value;

        args["x-message-ttl"] = ttlMs;
        args["x-dead-letter-exchange"] = string.Empty;
        args["x-dead-letter-routing-key"] = settings.InboundQueueName;

        return channel.QueueDeclareAsync(
            queue: delayQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args,
            cancellationToken: cancellationToken);
    }
}

