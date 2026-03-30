using InteractiveLeads.Application.Feature.Chat.Messages.Outbound;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Messaging.Contracts;
using InteractiveLeads.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace InteractiveLeads.Infrastructure.Messaging;

/// <summary>
/// Publishes outbound payloads to the configured exchange (fanout → quorum queue).
/// </summary>
public sealed class RabbitMqOutboundMessagePublisher(
    IRabbitMqConnectionFactoryProvider connectionFactoryProvider,
    IOptions<RabbitMqSettings> options) : IOutboundMessagePublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task PublishAsync(OutboundMessageContract contract, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        await using var connection = await connectionFactoryProvider.Create().CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await DeclareOutboundTopologyAsync(channel, settings, cancellationToken);

        // Publish the contract directly (no { message: ... } wrapper).
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(contract, JsonOptions));

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await channel.BasicPublishAsync(
            exchange: settings.OutboundExchangeName,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);
    }

    private static async Task DeclareOutboundTopologyAsync(IChannel channel, RabbitMqSettings settings, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: settings.OutboundExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var queueArgs = settings.UseQuorumQueues
            ? new Dictionary<string, object?> { ["x-queue-type"] = "quorum" }
            : null;

        await channel.QueueDeclareAsync(
            queue: settings.OutboundQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: settings.OutboundQueueName,
            exchange: settings.OutboundExchangeName,
            routingKey: string.Empty,
            cancellationToken: cancellationToken);
    }
}
