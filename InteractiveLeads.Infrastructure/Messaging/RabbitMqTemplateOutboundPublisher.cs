using System.Text;
using System.Text.Json;
using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.TemplateQueue;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace InteractiveLeads.Infrastructure.Messaging;

public sealed class RabbitMqTemplateOutboundPublisher(
    IRabbitMqConnectionFactoryProvider connectionFactoryProvider,
    IOptions<RabbitMqSettings> options) : ITemplateOutboundPublisher
{
    public bool PublishesToBroker => true;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public async Task PublishCreateTemplateAsync(TemplateCreateOutboundMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        await using var connection = await connectionFactoryProvider.Create().CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await DeclareTemplateOutboundTopologyAsync(channel, settings, cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await channel.BasicPublishAsync(
            exchange: settings.TemplateOutboundExchangeName,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);
    }

    private static async Task DeclareTemplateOutboundTopologyAsync(
        IChannel channel,
        RabbitMqSettings settings,
        CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: settings.TemplateOutboundExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var queueArgs = settings.UseQuorumQueues
            ? new Dictionary<string, object?> { ["x-queue-type"] = "quorum" }
            : null;

        await channel.QueueDeclareAsync(
            queue: settings.TemplateOutboundQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: settings.TemplateOutboundQueueName,
            exchange: settings.TemplateOutboundExchangeName,
            routingKey: string.Empty,
            cancellationToken: cancellationToken);
    }
}
