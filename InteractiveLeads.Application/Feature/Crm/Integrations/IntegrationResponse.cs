using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Feature.Crm.Integrations;

public sealed class IntegrationResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public IntegrationType Provider { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Internal CRM id for the linked WABA (templates scope). Null if <c>businessAccountId</c> was never set.</summary>
    public Guid? WhatsAppBusinessAccountId { get; set; }

    public object? Settings { get; set; }
}

