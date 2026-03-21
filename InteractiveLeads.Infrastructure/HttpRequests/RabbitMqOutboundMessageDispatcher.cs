using InteractiveLeads.Application.Feature.Chat.Messages.Outbound;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.Extensions.Logging;

namespace InteractiveLeads.Infrastructure.HttpRequests;

/// <summary>Publishes outbound payloads to the configured RabbitMQ outbound queue.</summary>
public sealed class RabbitMqOutboundMessageDispatcher(
    IOutboundMessagePublisher outboundPublisher,
    ILogger<RabbitMqOutboundMessageDispatcher> logger) : IOutboundMessageDispatcher
{
    public async Task<BaseResponse> SendMessageAsync(OutboundMessageContract payload, CancellationToken cancellationToken)
    {
        var response = new BaseResponse();
        try
        {
            await outboundPublisher.PublishAsync(payload, cancellationToken);
            return response;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "RabbitMQ outbound dispatch failed for provider {Provider}, channel {ChannelId}, messageId {MessageId}",
                payload.Provider,
                payload.ChannelId,
                payload.Payload.Id);
            response.AddExceptionMessage(
                "Error while dispatching outbound message to the message broker.",
                "chat.message.outbound_broker_dispatch_error");
            return response;
        }
    }
}
