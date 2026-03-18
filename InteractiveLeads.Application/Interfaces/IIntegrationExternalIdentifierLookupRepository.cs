using InteractiveLeads.Application.Feature.Integrations;
using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Interfaces;

/// <summary>
/// Host DB: resolves integration external id to tenant for public webhooks.
/// </summary>
public interface IIntegrationExternalIdentifierLookupRepository
{
    Task<IntegrationExternalIdentifierLookupModel?> GetByProviderAndExternalIdentifierAsync(
        IntegrationType type,
        string externalIdentifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces any row for <paramref name="integrationId"/> and inserts the new mapping.
    /// </summary>
    Task UpsertAsync(
        string tenantId,
        Guid integrationId,
        IntegrationType type,
        string externalIdentifier,
        CancellationToken cancellationToken = default);

    Task RemoveByIntegrationIdAsync(Guid integrationId, CancellationToken cancellationToken = default);
}
