using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.TemplateQueue;

namespace InteractiveLeads.Application.Interfaces;

/// <summary>Publishes WhatsApp template jobs to the template outbound exchange/queue.</summary>
public interface ITemplateOutboundPublisher
{
    /// <summary>False when RabbitMQ integration is disabled (no message is sent).</summary>
    bool PublishesToBroker { get; }

    Task PublishCreateTemplateAsync(TemplateCreateOutboundMessage message, CancellationToken cancellationToken);
}
