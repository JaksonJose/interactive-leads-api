using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Infrastructure.Tenancy.Models;

/// <summary>
/// Host DB lookup: maps (integration type + external identifier) to tenant + CRM integration id
/// so public webhooks (e.g. n8n) can resolve tenant without header.
/// </summary>
public sealed class IntegrationLookup
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public IntegrationType IntegrationType { get; set; }
    public string ExternalIdentifier { get; set; } = string.Empty;
    public Guid IntegrationId { get; set; }
    public DateTime CreatedAt { get; set; }
}
