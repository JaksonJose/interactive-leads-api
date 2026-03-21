using System.Text.Json;
using InteractiveLeads.Application.Feature.Inbound;
using InteractiveLeads.Application.Feature.Inbound.Messages;
using InteractiveLeads.Application.Messaging.Contracts;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Infrastructure.Configuration;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InteractiveLeads.Infrastructure.Messaging;

public sealed class InboundIntegrationEventConsumer(
    IServiceProvider serviceProvider,
    ILogger<InboundIntegrationEventConsumer> logger) : IConsumer<InboundIntegrationEvent>
{
    private static readonly JsonSerializerOptions LogJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task Consume(ConsumeContext<InboundIntegrationEvent> context)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<RabbitMqSettings>>().Value;

        var evt = context.Message.NormalizedEvent;
        var messageId = TryGetMessageId(evt.Payload);

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["Inbound.Provider"] = evt.Provider,
            ["Inbound.ExternalIdentifier"] = evt.Identifications?.ExternalIdentifier,
            ["Inbound.MessageId"] = messageId,
            ["MassTransit.RetryAttempt"] = GetRetryAttempt(context),
            ["MassTransit.RedeliveryCount"] = GetRedeliveryCount(context)
        }))
        {
            try
            {
                var response = await sender.Send(
                    new ProcessInboundEventCommand
                    {
                        Event = evt,
                        ReliableMessaging = true
                    },
                    context.CancellationToken);

                if (response is not SingleResponse<InboundProcessingResultDto> single || single.Data == null)
                {
                    logger.LogError("Inbound handler returned unexpected response type {Type}", response?.GetType().Name);
                    throw new InvalidOperationException("Unexpected MediatR response for inbound event.");
                }

                var data = single.Data;

                switch (data.Outcome)
                {
                    case InboundProcessingOutcome.Persisted:
                    case InboundProcessingOutcome.DuplicateIgnored:
                        logger.LogInformation(
                            "Inbound message acknowledged. Outcome {Outcome} Reason {Reason} Processed {Processed}",
                            data.Outcome,
                            data.Reason,
                            data.Processed);
                        break;

                    case InboundProcessingOutcome.PermanentRejected:
                        logger.LogWarning(
                            "Inbound message permanently rejected (ACK). Reason {Reason} Outcome {Outcome}",
                            data.Reason,
                            data.Outcome);

                        if (settings.ForwardPermanentRejectionsToUnprocessedQueue)
                            await ForwardToUnprocessedQueueAsync(context, settings, data, evt, messageId);
                        break;

                    case InboundProcessingOutcome.TransientRetry:
                        logger.LogError(
                            "Inbound handler returned TransientRetry with ReliableMessaging=true (bug). Reason {Reason}",
                            data.Reason);
                        throw new InvalidOperationException("TransientRetry must not be returned when ReliableMessaging is true.");

                    default:
                        if (!data.Processed)
                        {
                            logger.LogError(
                                "Inbound outcome unknown and not processed. Outcome {Outcome} Reason {Reason}",
                                data.Outcome,
                                data.Reason);
                            throw new InvalidOperationException($"Unhandled inbound outcome: {data.Outcome}");
                        }

                        break;
                }
            }
            catch (InboundTransientException ex)
            {
                logger.LogWarning(
                    ex,
                    "Inbound transient failure; will retry/redeliver. ReasonCode {ReasonCode} provider {Provider} externalId {ExternalId}",
                    ex.ReasonCode,
                    evt.Provider,
                    evt.Identifications?.ExternalIdentifier);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Inbound queue consumer failed for provider {Provider} externalId {ExternalId}",
                    evt.Provider,
                    evt.Identifications?.ExternalIdentifier);
                throw;
            }
        }
    }

    private static string? TryGetMessageId(System.Text.Json.JsonElement payload)
    {
        try
        {
            if (payload.ValueKind != JsonValueKind.Object)
                return null;
            if (!payload.TryGetProperty("id", out var idProp))
                return null;
            return idProp.GetString();
        }
        catch
        {
            return null;
        }
    }

    private static int GetRetryAttempt(ConsumeContext context) => context.GetRetryAttempt();

    private static int GetRedeliveryCount(ConsumeContext context)
    {
        if (context.Headers.TryGetHeader("MT-Redelivery-Count", out object? value) && value != null)
        {
            if (value is int i)
                return i;
            if (int.TryParse(value.ToString(), out var parsed))
                return parsed;
        }

        return 0;
    }

    private async Task ForwardToUnprocessedQueueAsync(
        ConsumeContext<InboundIntegrationEvent> context,
        RabbitMqSettings settings,
        InboundProcessingResultDto data,
        NormalizedInboundEvent evt,
        string? messageId)
    {
        try
        {
            var endpoint = await context.GetSendEndpoint(
                new Uri($"queue:{settings.UnprocessedQueueName}"));

            var raw = JsonSerializer.Serialize(context.Message, LogJsonOptions);

            await endpoint.Send(
                new InboundUnprocessedDispatch
                {
                    ReasonCode = data.Reason ?? "unknown",
                    Provider = evt.Provider,
                    ExternalIdentifier = evt.Identifications?.ExternalIdentifier,
                    MessageId = messageId,
                    RawEventJson = raw
                },
                context.CancellationToken);

            logger.LogInformation(
                "Forwarded permanent rejection to unprocessed queue {Queue} Reason {Reason}",
                settings.UnprocessedQueueName,
                data.Reason);
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
