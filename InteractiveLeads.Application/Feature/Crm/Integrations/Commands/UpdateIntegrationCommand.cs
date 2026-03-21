using System.Text.Json;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Integrations.Settings;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Integrations.Commands;

public sealed class UpdateIntegrationCommand : IRequest<IResponse>
{
    public Guid IntegrationId { get; set; }

    public UpdateIntegrationRequest Integration { get; set; } = new();
}

public sealed class UpdateIntegrationCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IIntegrationSettingsResolver settingsResolver,
    IIntegrationExternalIdentifierResolver externalIdentifierResolver,
    IIntegrationExternalIdentifierLookupRepository integrationLookupRepository) : IRequestHandler<UpdateIntegrationCommand, IResponse>
{
    public async Task<IResponse> Handle(UpdateIntegrationCommand request, CancellationToken cancellationToken)
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

        var integration = await db.Integrations
            .Where(i => i.Id == request.IntegrationId && i.CompanyId == companyId)
            .SingleOrDefaultAsync(cancellationToken);

        if (integration == null)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Integration not found.", "general.not_found");
            throw new NotFoundException(response);
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

        // Provider is not updatable for now; keep existing integration.Type
        var provider = integration.Type;
        if (!Enum.IsDefined(typeof(IntegrationType), provider) || provider != IntegrationType.WhatsApp)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Unsupported integration provider.", "integrations.unsupported_provider");
            throw new BadRequestException(response);
        }

        // Load existing settings
        var existingSettings = settingsResolver.Deserialize(provider, integration.Settings);
        if (existingSettings is not WhatsAppSettings existingWhatsapp)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Invalid stored settings for WhatsApp integration.", "integrations.invalid_settings_type");
            throw new BadRequestException(response);
        }

        WhatsAppSettings updatedSettings;

        if (request.Integration.Settings.ValueKind == JsonValueKind.Undefined
            || request.Integration.Settings.ValueKind == JsonValueKind.Null)
        {
            updatedSettings = existingWhatsapp;
        }
        else
        {
            var deserialized = settingsResolver.Deserialize(provider, request.Integration.Settings.GetRawText());
            if (deserialized is not WhatsAppSettings incomingWhatsapp)
            {
                var response = new ResultResponse();
                response.AddErrorMessage("Invalid settings payload for WhatsApp integration.", "integrations.invalid_settings_type");
                throw new BadRequestException(response);
            }

            updatedSettings = new WhatsAppSettings
            {
                AccessToken = string.IsNullOrWhiteSpace(incomingWhatsapp.AccessToken)
                    ? existingWhatsapp.AccessToken
                    : incomingWhatsapp.AccessToken,
                PhoneNumberId = string.IsNullOrWhiteSpace(incomingWhatsapp.PhoneNumberId)
                    ? existingWhatsapp.PhoneNumberId
                    : incomingWhatsapp.PhoneNumberId,
                BusinessAccountId = string.IsNullOrWhiteSpace(incomingWhatsapp.BusinessAccountId)
                    ? existingWhatsapp.BusinessAccountId
                    : incomingWhatsapp.BusinessAccountId,
                WebhookVerifyToken = string.IsNullOrWhiteSpace(incomingWhatsapp.WebhookVerifyToken)
                    ? existingWhatsapp.WebhookVerifyToken
                    : incomingWhatsapp.WebhookVerifyToken
            };
        }

        if (string.IsNullOrWhiteSpace(updatedSettings.AccessToken))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Access token is required for WhatsApp integrations.", "integrations.whatsapp.access_token_required");
            throw new BadRequestException(response);
        }

        var externalIdentifier = externalIdentifierResolver.ResolveExternalIdentifier(provider, updatedSettings);

        var hasDuplicate = await db.Integrations
            .AsNoTracking()
            .AnyAsync(i =>
                i.Id != integration.Id &&
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
        if (externalIdentifier != integration.ExternalIdentifier)
        {
            var taken = await integrationLookupRepository.GetByProviderAndExternalIdentifierAsync(
                provider, externalIdentifier, cancellationToken);
            if (taken != null && taken.IntegrationId != integration.Id)
            {
                if (taken.TenantId != finbuckleTenantId)
                {
                    var conflictResponse = new ResultResponse();
                    conflictResponse.AddErrorMessage(
                        "This external identifier is already registered to another workspace.",
                        "integrations.external_identifier_tenant_conflict");
                    throw new BadRequestException(conflictResponse);
                }

                var dupResponse = new ResultResponse();
                dupResponse.AddErrorMessage(
                    "This external identifier is already registered.",
                    "integrations.external_identifier_global_conflict");
                throw new BadRequestException(dupResponse);
            }
        }

        integration.Name = name;
        integration.IsActive = request.Integration.IsActive;
        integration.ExternalIdentifier = externalIdentifier;
        integration.Settings = settingsResolver.Serialize(provider, updatedSettings);

        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await integrationLookupRepository.UpsertAsync(
                finbuckleTenantId, integration.Id, provider, externalIdentifier, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            // Repository throws with a concrete reason (e.g. identifier already mapped).
            // Preserve it so clients/logs identify the failure; do not replace with a generic message.
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

