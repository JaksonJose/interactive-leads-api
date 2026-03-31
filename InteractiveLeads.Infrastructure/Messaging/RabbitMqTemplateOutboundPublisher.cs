using System.Text;
using System.Text.Json;
using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.TemplateQueue;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace InteractiveLeads.Infrastructure.Messaging;

public sealed class RabbitMqTemplateOutboundPublisher(
    IRabbitMqConnectionFactoryProvider connectionFactoryProvider,
    IOptions<RabbitMqSettings> options,
    ILogger<RabbitMqTemplateOutboundPublisher> logger) : ITemplateOutboundPublisher
{
    public bool PublishesToBroker => true;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public Task PublishCreateTemplateAsync(TemplateCreateOutboundMessage message, CancellationToken cancellationToken) =>
        PublishJsonToTemplateOutboundAsync(message, cancellationToken);

    public Task PublishUpdateTemplateAsync(TemplateUpdateOutboundMessage message, CancellationToken cancellationToken) =>
        PublishJsonToTemplateOutboundAsync(message, cancellationToken);

    public Task PublishDeleteTemplateAsync(TemplateDeleteOutboundMessage message, CancellationToken cancellationToken) =>
        PublishJsonToTemplateOutboundAsync(message, cancellationToken);

    public Task PublishTemplateSyncedAsync(TemplateSyncedOutboundMessage message, CancellationToken cancellationToken) =>
        PublishJsonToTemplateOutboundAsync(message, cancellationToken);

    private async Task PublishJsonToTemplateOutboundAsync(object message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        try
        {
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
        catch (Exception ex)
        {
            // Most common cause: PRECONDITION_FAILED due to existing queue declared with different args (classic vs quorum).
            logger.LogError(
                ex,
                "RabbitMQ publish to template outbound failed. exchange={Exchange} queue={Queue} vhost={VHost} useQuorum={UseQuorum}. " +
                "If you recently changed UseQuorumQueues, delete/recreate the queue or revert the setting.",
                settings.TemplateOutboundExchangeName,
                settings.TemplateOutboundQueueName,
                settings.VirtualHost,
                settings.UseQuorumQueues);
            throw;
        }
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
