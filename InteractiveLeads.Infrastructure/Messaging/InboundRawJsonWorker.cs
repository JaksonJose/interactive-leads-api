using System.Text;
using System.Text.Json;
using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Feature.Inbound;
using InteractiveLeads.Application.Feature.Inbound.Messages;
using InteractiveLeads.Application.Messaging.Contracts;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InteractiveLeads.Infrastructure.Messaging;

public sealed class InboundRawJsonWorker(
    IRabbitMqConnectionFactoryProvider connectionFactoryProvider,
    IServiceProvider serviceProvider,
    IOptions<RabbitMqSettings> rabbitOptions,
    ILogger<InboundRawJsonWorker> logger) : BackgroundService
{
    private const string RedeliveryHeader = "x-redelivery-count";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly TimeSpan[] ImmediateRetries =
    [
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5)
    ];

    private static readonly (string Suffix, TimeSpan Delay)[] DelayedRedelivery =
    [
        ("30s", TimeSpan.FromSeconds(30)),
        ("2m", TimeSpan.FromMinutes(2)),
        ("10m", TimeSpan.FromMinutes(10))
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = rabbitOptions.Value;

        await using var connection = await connectionFactoryProvider.Create().CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 8, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            await HandleDeliveryAsync(channel, settings, ea, stoppingToken);
        };

        await channel.BasicConsumeAsync(
            queue: settings.InboundQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        // Keep running until cancelled.
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task HandleDeliveryAsync(
        IChannel channel,
        RabbitMqSettings settings,
        BasicDeliverEventArgs ea,
        CancellationToken stoppingToken)
    {
        var bodyBytes = ea.Body.ToArray();
        var bodyText = Encoding.UTF8.GetString(bodyBytes);

        InboundIntegrationEvent? inbound;
        try
        {
            inbound = JsonSerializer.Deserialize<InboundIntegrationEvent>(bodyText, JsonOptions);
            if (inbound is null)
                throw new JsonException("Inbound JSON deserialized to null.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Invalid inbound JSON; moving to error queue. queue {Queue}", settings.InboundQueueName);
            await PublishToQueueAsync(channel, $"{settings.InboundQueueName}.error", bodyBytes, ea.BasicProperties, stoppingToken);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            return;
        }

        var normalized = inbound.NormalizedEvent;
        var messageId = TryGetMessageId(normalized.Payload);
        var redeliveryCount = GetHeaderInt(ea.BasicProperties?.Headers, RedeliveryHeader);

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["Inbound.Provider"] = normalized.Provider,
            ["Inbound.ExternalIdentifier"] = normalized.Identifications?.ExternalIdentifier,
            ["Inbound.MessageId"] = messageId,
            ["Inbound.RedeliveryCount"] = redeliveryCount
        }))
        {
            var (ok, permanentReject, rejectReason) = await TryProcessWithRetriesAsync(inbound, stoppingToken);
            if (ok)
            {
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                return;
            }

            if (permanentReject)
            {
                logger.LogWarning("Inbound message permanently rejected (ACK). Reason {Reason}", rejectReason);

                if (settings.ForwardPermanentRejectionsToUnprocessedQueue)
                {
                    await TryForwardToUnprocessedQueueAsync(
                        channel,
                        settings,
                        rejectReason ?? "unknown",
                        normalized,
                        messageId,
                        inbound,
                        stoppingToken);
                }

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                return;
            }

            // Transient failure: schedule delayed redelivery or dead-letter to error.
            if (redeliveryCount < DelayedRedelivery.Length)
            {
                var next = DelayedRedelivery[redeliveryCount];
                var delayQueue = $"{settings.InboundQueueName}.delay.{next.Suffix}";

                logger.LogWarning("Inbound transient failure; scheduling delayed redelivery {Delay}.", next.Delay);

                var props = CloneBasicProperties(ea.BasicProperties);
                props.Headers ??= new Dictionary<string, object?>();
                props.Headers[RedeliveryHeader] = redeliveryCount + 1;

                await PublishToQueueAsync(channel, delayQueue, bodyBytes, props, stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                return;
            }

            logger.LogError("Inbound transient failure exhausted; moving to error queue.");
            await PublishToQueueAsync(channel, $"{settings.InboundQueueName}.error", bodyBytes, ea.BasicProperties, stoppingToken);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
        }
    }

    private async Task<(bool Ok, bool PermanentReject, string? Reason)> TryProcessWithRetriesAsync(
        InboundIntegrationEvent inbound,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var result = await ProcessOnceAsync(inbound, cancellationToken);
                return result;
            }
            catch (InboundTransientException ex)
            {
                if (attempt >= ImmediateRetries.Length)
                    return (false, false, ex.ReasonCode);

                logger.LogWarning(ex, "Inbound transient failure; immediate retry in {Delay}.", ImmediateRetries[attempt]);
                await Task.Delay(ImmediateRetries[attempt], cancellationToken);
            }
            catch (Exception ex)
            {
                if (attempt >= ImmediateRetries.Length)
                    return (false, false, "exception");

                logger.LogWarning(ex, "Inbound failure; immediate retry in {Delay}.", ImmediateRetries[attempt]);
                await Task.Delay(ImmediateRetries[attempt], cancellationToken);
            }
        }
    }

    private async Task<(bool Ok, bool PermanentReject, string? Reason)> ProcessOnceAsync(
        InboundIntegrationEvent inbound,
        CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<IRequestDispatcher>();

        var response = await sender.Send(
            new ProcessInboundEventCommand
            {
                Event = inbound.NormalizedEvent,
                ReliableMessaging = true
            },
            cancellationToken);

        if (response is not SingleResponse<InboundProcessingResultDto> single || single.Data == null)
        {
            logger.LogError("Inbound handler returned unexpected response type {Type}", response?.GetType().Name);
            throw new InvalidOperationException("Unexpected request dispatcher response for inbound event.");
        }

        var data = single.Data;
        return data.Outcome switch
        {
            InboundProcessingOutcome.Persisted => (true, false, null),
            InboundProcessingOutcome.DuplicateIgnored => (true, false, null),
            InboundProcessingOutcome.PermanentRejected => (false, true, data.Reason),
            InboundProcessingOutcome.TransientRetry => throw new InvalidOperationException(
                "TransientRetry must not be returned when ReliableMessaging is true."),
            _ when !data.Processed => throw new InvalidOperationException($"Unhandled inbound outcome: {data.Outcome}"),
            _ => (true, false, null)
        };
    }

    private static string? TryGetMessageId(JsonElement payload)
    {
        try
        {
            if (payload.ValueKind != JsonValueKind.Object)
                return null;
            if (payload.TryGetProperty("id", out var idProp))
                return idProp.GetString();
            if (payload.TryGetProperty("messageId", out var messageIdProp))
                return messageIdProp.GetString();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static int GetHeaderInt(IDictionary<string, object?>? headers, string key)
    {
        if (headers == null) return 0;
        if (!headers.TryGetValue(key, out var value) || value == null) return 0;

        return value switch
        {
            int i => i,
            long l => (int)l,
            byte b => b,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            _ when int.TryParse(value.ToString(), out var parsed) => parsed,
            _ => 0
        };
    }

    private static BasicProperties CloneBasicProperties(IReadOnlyBasicProperties? source)
    {
        var props = new BasicProperties
        {
            ContentType = source?.ContentType,
            CorrelationId = source?.CorrelationId,
            DeliveryMode = source?.DeliveryMode ?? DeliveryModes.Persistent,
            MessageId = source?.MessageId,
            Type = source?.Type,
            Timestamp = source?.Timestamp ?? new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        if (source?.Headers != null)
        {
            props.Headers = new Dictionary<string, object?>();
            foreach (var kvp in source.Headers)
                props.Headers[kvp.Key] = kvp.Value;
        }

        return props;
    }

    private static Task PublishToQueueAsync(
        IChannel channel,
        string queue,
        byte[] body,
        IReadOnlyBasicProperties? props,
        CancellationToken cancellationToken)
    {
        var publishProps = props is BasicProperties bp ? bp : CloneBasicProperties(props);
        publishProps.ContentType ??= "application/json";
        publishProps.DeliveryMode = DeliveryModes.Persistent;

        // Default exchange routes by queue name.
        return channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queue,
            mandatory: false,
            basicProperties: publishProps,
            body: body,
            cancellationToken: cancellationToken).AsTask();
    }

    private async Task TryForwardToUnprocessedQueueAsync(
        IChannel channel,
        RabbitMqSettings settings,
        string reasonCode,
        NormalizedInboundEvent evt,
        string? messageId,
        InboundIntegrationEvent original,
        CancellationToken cancellationToken)
    {
        try
        {
            var dispatch = new InboundUnprocessedDispatch
            {
                ReasonCode = reasonCode,
                Provider = evt.Provider,
                ExternalIdentifier = evt.Identifications?.ExternalIdentifier,
                MessageId = messageId,
                RawEventJson = JsonSerializer.Serialize(original, JsonOptions)
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dispatch, JsonOptions));
            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: settings.UnprocessedQueueName,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Forwarded permanent rejection to unprocessed queue {Queue} Reason {Reason}",
                settings.UnprocessedQueueName,
                reasonCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to forward to unprocessed queue {Queue}; message was still ACKd.",
                settings.UnprocessedQueueName);
        }
    }
}

