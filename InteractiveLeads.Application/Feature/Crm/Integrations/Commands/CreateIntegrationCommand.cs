using System.Text.Json;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Integrations.Settings;
using InteractiveLeads.Application.Integrations.WhatsApp;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Domain.Enums;
using InteractiveLeads.Application.Dispatching;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Integrations.Commands;

public sealed class CreateIntegrationCommand : IApplicationRequest<IResponse>
{
    public CreateIntegrationRequest Integration { get; set; } = new();
}

public sealed class CreateIntegrationCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IIntegrationSettingsResolver settingsResolver,
    IIntegrationExternalIdentifierResolver externalIdentifierResolver,
    IIntegrationExternalIdentifierLookupRepository integrationLookupRepository,
    IWhatsAppBusinessAccountLinker wabaLinker) : IApplicationRequestHandler<CreateIntegrationCommand, IResponse>
{
    public async Task<IResponse> Handle(CreateIntegrationCommand request, CancellationToken cancellationToken)
    {
        var tenantIdentifier = currentUserService.GetUserTenant();
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            var badRequest = new ResultResponse();
            badRequest.AddErrorMessage("Tenant context is required.", "general.bad_request");
            throw new BadRequestException(badRequest);
        }

        var crmTenantId = await db.Tenants
            .Where(t => t.Identifier == tenantIdentifier)
            .Select(t => t.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (crmTenantId == Guid.Empty)
        {
            var notFound = new ResultResponse();
            notFound.AddErrorMessage("CRM tenant not found.", "general.not_found");
            throw new NotFoundException(notFound);
        }

        var companyId = await db.Companies
            .Where(c => c.TenantId == crmTenantId)
            .Select(c => c.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (companyId == Guid.Empty)
        {
            var notFound = new ResultResponse();
            notFound.AddErrorMessage("Company not found for current tenant.", "general.not_found");
            throw new NotFoundException(notFound);
        }

        var name = (request.Integration?.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Integration name is required.", "integrations.name_required");
            throw new BadRequestException(response);
        }

        if (name.Length > 256)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Integration name is too long.", "integrations.name_too_long");
            throw new BadRequestException(response);
        }

        var provider = request.Integration.Provider;
        if (!Enum.IsDefined(typeof(IntegrationType), provider) || provider != IntegrationType.WhatsApp)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Unsupported integration provider.", "integrations.unsupported_provider");
            throw new BadRequestException(response);
        }

        if (request.Integration.Settings.ValueKind == JsonValueKind.Undefined
            || request.Integration.Settings.ValueKind == JsonValueKind.Null)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Settings payload is required for WhatsApp integration.", "integrations.settings_required");
            throw new BadRequestException(response);
        }

        var deserializedSettings = settingsResolver.Deserialize(provider, request.Integration.Settings.GetRawText());
        if (deserializedSettings is not WhatsAppSettings whatsappSettings)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Invalid settings payload for WhatsApp integration.", "integrations.invalid_settings_type");
            throw new BadRequestException(response);
        }

        if (string.IsNullOrWhiteSpace(whatsappSettings.AccessToken))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Access token is required for WhatsApp integrations.", "integrations.whatsapp.access_token_required");
            throw new BadRequestException(response);
        }

        var externalIdentifier = externalIdentifierResolver.ResolveExternalIdentifier(provider, whatsappSettings);

        var hasDuplicate = await db.Integrations
            .AsNoTracking()
            .AnyAsync(i =>
                i.CompanyId == companyId &&
                i.Type == provider &&
                i.ExternalIdentifier == externalIdentifier,
                cancellationToken);

        if (hasDuplicate)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("An integration for this provider and external identifier already exists.", "integrations.duplicate_external_identifier");
            throw new BadRequestException(response);
        }

        var finbuckleTenantId = currentUserService.GetUserTenant();
        var existingGlobal = await integrationLookupRepository.GetByProviderAndExternalIdentifierAsync(
            provider, externalIdentifier, cancellationToken);
        if (existingGlobal != null)
        {
            if (existingGlobal.TenantId != finbuckleTenantId)
            {
                var conflictResponse = new ResultResponse();
                conflictResponse.AddErrorMessage(
                    "This external identifier is already registered to another workspace.",
                    "integrations.external_identifier_tenant_conflict");
                throw new BadRequestException(conflictResponse);
            }

            var stillExists = await db.Integrations
                .AsNoTracking()
                .AnyAsync(i => i.Id == existingGlobal.IntegrationId, cancellationToken);
            if (stillExists)
            {
                var dupResponse = new ResultResponse();
                dupResponse.AddErrorMessage(
                    "This external identifier is already registered.",
                    "integrations.external_identifier_global_conflict");
                throw new BadRequestException(dupResponse);
            }

            await integrationLookupRepository.RemoveByIntegrationIdAsync(
                existingGlobal.IntegrationId, cancellationToken);
        }

        var settingsJson = settingsResolver.Serialize(provider, whatsappSettings);

        var wabaId = await wabaLinker.EnsureWabaIdAsync(companyId, whatsappSettings.BusinessAccountId, cancellationToken);

        var integration = new Integration
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Type = provider,
            Name = name,
            ExternalIdentifier = externalIdentifier,
            Settings = settingsJson,
            IsActive = request.Integration.IsActive,
            CreatedAt = DateTimeOffset.UtcNow,
            WhatsAppBusinessAccountId = wabaId
        };

        db.Integrations.Add(integration);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await integrationLookupRepository.UpsertAsync(
                finbuckleTenantId, integration.Id, provider, externalIdentifier, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            db.Integrations.Remove(integration);
            await db.SaveChangesAsync(cancellationToken);
            var raceResponse = new ResultResponse();
            raceResponse.AddErrorMessage(
                ex.Message,
                "integrations.external_identifier_global_conflict");
            throw new BadRequestException(raceResponse);
        }

        var responseDto = IntegrationDtoMapper.ToResponse(integration, settingsResolver);
        return new SingleResponse<IntegrationResponse>(responseDto);
    }
}


