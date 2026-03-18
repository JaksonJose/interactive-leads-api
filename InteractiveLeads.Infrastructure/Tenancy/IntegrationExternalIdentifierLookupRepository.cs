using InteractiveLeads.Application.Feature.Integrations;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Domain.Enums;
using InteractiveLeads.Infrastructure.Context.Tenancy;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Tenancy;

public sealed class IntegrationLookupRepository(TenantDbContext context)
    : IIntegrationExternalIdentifierLookupRepository
{
    public async Task<IntegrationExternalIdentifierLookupModel?> GetByProviderAndExternalIdentifierAsync(
        IntegrationType type,
        string externalIdentifier,
        CancellationToken cancellationToken = default)
    {
        var entity = await context.IntegrationLookups
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.IntegrationType == type && x.ExternalIdentifier == externalIdentifier,
                cancellationToken);
        return entity == null ? null : Map(entity);
    }

    public async Task UpsertAsync(
        string tenantId,
        Guid integrationId,
        IntegrationType type,
        string externalIdentifier,
        CancellationToken cancellationToken = default)
    {
        var existingForIntegration = await context.IntegrationLookups
            .FirstOrDefaultAsync(x => x.IntegrationId == integrationId, cancellationToken);
        if (existingForIntegration != null)
            context.IntegrationLookups.Remove(existingForIntegration);

        var taken = await context.IntegrationLookups
            .FirstOrDefaultAsync(
                x => x.IntegrationType == type && x.ExternalIdentifier == externalIdentifier,
                cancellationToken);
        if (taken != null)
            throw new InvalidOperationException(
                $"External identifier '{externalIdentifier}' is already mapped to another integration.");

        context.IntegrationLookups.Add(new IntegrationLookup
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            IntegrationType = type,
            ExternalIdentifier = externalIdentifier,
            IntegrationId = integrationId,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveByIntegrationIdAsync(Guid integrationId, CancellationToken cancellationToken = default)
    {
        var rows = await context.IntegrationLookups
            .Where(x => x.IntegrationId == integrationId)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return;
        context.IntegrationLookups.RemoveRange(rows);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IntegrationExternalIdentifierLookupModel Map(IntegrationLookup e) =>
        new()
        {
            TenantId = e.TenantId,
            IntegrationId = e.IntegrationId,
            IntegrationType = e.IntegrationType,
            ExternalIdentifier = e.ExternalIdentifier
        };
}
