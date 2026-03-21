using InteractiveLeads.Application.Feature.Webhooks.Messages;
using InteractiveLeads.Application.Messaging.Contracts;
using InteractiveLeads.Application.Responses;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InteractiveLeads.Infrastructure.Messaging;

public sealed class InboundIntegrationEventConsumer(
    IServiceProvider serviceProvider,
    ILogger<InboundIntegrationEventConsumer> logger) : IConsumer<InboundIntegrationEvent>
{
    public async Task Consume(ConsumeContext<InboundIntegrationEvent> context)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        try
        {
            var response = await sender.Send(
                new ProcessWebhookEventCommand { Event = context.Message.Event },
                context.CancellationToken);

            if (response is SingleResponse<WebhookProcessingResultDto> single &&
                single.Data is { Processed: false } data)
            {
                logger.LogWarning(
                    "Inbound message acknowledged without persisting conversation/message. Reason {Reason} provider {Provider} externalId {ExternalId}",
                    data.Reason,
                    context.Message.Event.Provider,
                    context.Message.Event.Identifications?.ExternalIdentifier);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Inbound queue consumer failed for provider {Provider} externalId {ExternalId}",
                context.Message.Event.Provider,
                context.Message.Event.Identifications?.ExternalIdentifier);
            throw;
        }
    }
}
