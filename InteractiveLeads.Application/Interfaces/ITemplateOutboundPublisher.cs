using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.TemplateQueue;

namespace InteractiveLeads.Application.Interfaces;

/// <summary>Publishes WhatsApp template jobs to the template outbound exchange/queue.</summary>
public interface ITemplateOutboundPublisher
{
    /// <summary>False when RabbitMQ integration is disabled (no message is sent).</summary>
    bool PublishesToBroker { get; }

    /// <summary>Queued template creation (<see cref="TemplateCreateOutboundMessage"/>).</summary>
    Task PublishCreateTemplateAsync(TemplateCreateOutboundMessage message, CancellationToken cancellationToken);

    /// <summary>Queued template update on Meta (<see cref="TemplateUpdateOutboundMessage"/>, <c>update_template</c>).</summary>
    Task PublishUpdateTemplateAsync(TemplateUpdateOutboundMessage message, CancellationToken cancellationToken);

    /// <summary>Queued template delete by name/language (<see cref="TemplateDeleteOutboundMessage"/>).</summary>
    Task PublishDeleteTemplateAsync(TemplateDeleteOutboundMessage message, CancellationToken cancellationToken);

    /// <summary>Queued template sync from Meta (<see cref="TemplateSyncedOutboundMessage"/>, <c>template_synced</c>).</summary>
    Task PublishTemplateSyncedAsync(TemplateSyncedOutboundMessage message, CancellationToken cancellationToken);
}
