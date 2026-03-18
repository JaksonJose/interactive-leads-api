using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Webhooks.Messages;

public sealed class ProcessWebhookEventCommand : IRequest<IResponse>
{
    public WebhookEventRequest Event { get; set; } = new();
}

