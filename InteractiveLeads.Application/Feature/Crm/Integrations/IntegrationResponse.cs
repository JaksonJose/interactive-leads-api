using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Feature.Crm.Integrations;

public sealed class WhatsAppBusinessAccountSummary
{
    public Guid Id { get; set; }

    /// <summary>Meta WABA id.</summary>
    public string WabaId { get; set; } = string.Empty;

    public string? Name { get; set; }
}

public sealed class IntegrationResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public IntegrationType Provider { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Internal CRM id for the linked WABA (templates scope). Null if <c>businessAccountId</c> was never set.</summary>
    public Guid? WhatsAppBusinessAccountId { get; set; }

    /// <summary>Linked WABA row when <see cref="WhatsAppBusinessAccountId"/> is set and navigation was loaded.</summary>
    public WhatsAppBusinessAccountSummary? WhatsAppBusinessAccount { get; set; }

    public object? Settings { get; set; }
}

