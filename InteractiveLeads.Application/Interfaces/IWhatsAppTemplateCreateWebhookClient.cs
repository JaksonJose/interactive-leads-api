using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.TemplateQueue;

namespace InteractiveLeads.Application.Interfaces;

/// <summary>POSTs <see cref="TemplateCreateOutboundMessage"/> to the workflow webhook (replaces RabbitMQ for creates).</summary>
public interface IWhatsAppTemplateCreateWebhookClient
{
    /// <exception cref="System.Net.Http.HttpRequestException">Non-success HTTP status from webhook.</exception>
    Task PostCreateAsync(TemplateCreateOutboundMessage message, CancellationToken cancellationToken);
}
