using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Feature.Crm.Integrations;

public sealed class IntegrationResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public IntegrationType Provider { get; set; }

    public bool IsActive { get; set; }

    public object? Settings { get; set; }
}

