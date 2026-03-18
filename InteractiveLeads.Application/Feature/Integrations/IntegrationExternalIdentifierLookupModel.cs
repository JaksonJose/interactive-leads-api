using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Feature.Integrations;

public sealed class IntegrationExternalIdentifierLookupModel
{
    public string TenantId { get; set; } = string.Empty;
    public Guid IntegrationId { get; set; }
    public IntegrationType IntegrationType { get; set; }
    public string ExternalIdentifier { get; set; } = string.Empty;
}
