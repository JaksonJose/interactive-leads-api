using System.Text;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InteractiveLeads.Infrastructure.Messaging;

/// <summary>Consumes <see cref="RabbitMqSettings.TemplateInboundQueueName"/> (<c>eventType: template</c>).</summary>
public sealed class TemplateInboundWorker(
    IRabbitMqConnectionFactoryProvider connectionFactoryProvider,
    IServiceProvider serviceProvider,
    IOptions<RabbitMqSettings> rabbitOptions,
    ILogger<TemplateInboundWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = rabbitOptions.Value;

        await using var connection = await connectionFactoryProvider.Create().CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 4, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var bodyBytes = ea.Body.ToArray();
            var bodyText = Encoding.UTF8.GetString(bodyBytes);

            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<ITemplateInboundMessageHandler>();
                var ok = await handler.TryHandleAsync(bodyText, stoppingToken);
                if (ok)
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    return;
                }

                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Template inbound handler failed; routing to error queue.");
                try
                {
                    var errProps = new BasicProperties
                    {
                        ContentType = "application/json",
                        DeliveryMode = DeliveryModes.Persistent
                    };
                    await channel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: $"{settings.TemplateInboundQueueName}.error",
                        mandatory: false,
                        basicProperties: errProps,
                        body: bodyBytes,
                        cancellationToken: stoppingToken);
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception ex2)
                {
                    logger.LogError(ex2, "Failed to forward template inbound message to error queue.");
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                }
            }
        };

        await channel.BasicConsumeAsync(
            queue: settings.TemplateInboundQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
