using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.TemplateQueue;
using InteractiveLeads.Application.Interfaces;

namespace InteractiveLeads.Infrastructure.Messaging;

/// <summary>Used when RabbitMQ is disabled; template jobs are not sent to Meta workers.</summary>
public sealed class NoopTemplateOutboundPublisher : ITemplateOutboundPublisher
{
    public bool PublishesToBroker => false;

    public Task PublishCreateTemplateAsync(TemplateCreateOutboundMessage message, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task PublishDeleteTemplateAsync(TemplateDeleteOutboundMessage message, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task PublishTemplateSyncedAsync(TemplateSyncedOutboundMessage message, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
