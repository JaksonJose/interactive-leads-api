using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Integrations.Settings;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Enums;
using InteractiveLeads.Application.Dispatching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Application.Feature.Integrations.Queries;

public sealed class IntegrationSettingsResponse
{
    // Intencional: consumidor sÃ³ precisa saber que credencial existe.
    // O acesso ao token real deve ser tratado com autenticaÃ§Ã£o no endpoint (futuramente).
    public string AccessToken { get; set; } = "******";

    public string PhoneNumberId { get; set; } = string.Empty;

    public string BusinessAccountId { get; set; } = string.Empty;

    public string WebhookVerifyToken { get; set; } = WhatsAppWebhookDefaults.FixedVerifyToken;
}

public sealed class IntegrationByIdentifierResponse
{
    public string Name { get; set; } = string.Empty;

    public IntegrationType Provider { get; set; }

    public bool IsActive { get; set; }

    public IntegrationSettingsResponse Settings { get; set; } = new();
}

public sealed class GetIntegrationByIdentifierQuery : IApplicationRequest<IntegrationByIdentifierResponse>
{
    public string Provider { get; set; } = string.Empty;

    public string Identifier { get; set; } = string.Empty;
}

public sealed class GetIntegrationByIdentifierQueryHandler(
    IIntegrationExternalIdentifierLookupRepository integrationLookupRepository,
    ICrossTenantService crossTenantService)
    : IApplicationRequestHandler<GetIntegrationByIdentifierQuery, IntegrationByIdentifierResponse>
{
    public async Task<IntegrationByIdentifierResponse> Handle(
        GetIntegrationByIdentifierQuery request,
        CancellationToken cancellationToken)
    {
        var externalIdentifier = request.Identifier?.Trim();
        if (string.IsNullOrWhiteSpace(externalIdentifier))
        {
            var badRequest = new ResultResponse();
            badRequest.AddErrorMessage("Identifier is required.", "integrations.identifier_required");
            throw new BadRequestException(badRequest);
        }

        var providerRaw = request.Provider?.Trim();
        if (string.IsNullOrWhiteSpace(providerRaw))
        {
            var badRequest = new ResultResponse();
            badRequest.AddErrorMessage("Provider is required.", "integrations.provider_required");
            throw new BadRequestException(badRequest);
        }

        if (!Enum.TryParse<IntegrationType>(providerRaw, ignoreCase: true, out var integrationType))
        {
            var badRequest = new ResultResponse();
            badRequest.AddErrorMessage("Unsupported integration provider.", "integrations.unsupported_provider");
            throw new BadRequestException(badRequest);
        }

        var lookup = await integrationLookupRepository.GetByProviderAndExternalIdentifierAsync(
            integrationType,
            externalIdentifier,
            cancellationToken);

        if (lookup == null)
        {
            var notFound = new ResultResponse();
            notFound.AddErrorMessage(
                "Integration not found for given provider and identifier.",
                "integrations.not_found");
            throw new NotFoundException(notFound);
        }

        IntegrationByIdentifierResponse? result = null;

        await crossTenantService.ExecuteInTenantContextForSystemAsync(lookup.TenantId, async sp =>
        {
            var db = sp.GetRequiredService<IApplicationDbContext>();
            var settingsResolver = sp.GetRequiredService<IIntegrationSettingsResolver>();

            var integration = await db.Integrations
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    i => i.Id == lookup.IntegrationId,
                    cancellationToken);

            if (integration == null)
            {
                var notFound = new ResultResponse();
                notFound.AddErrorMessage("Integration not found in tenant.", "general.not_found");
                throw new NotFoundException(notFound);
            }

            if (integrationType != IntegrationType.WhatsApp)
            {
                var response = new ResultResponse();
                response.AddErrorMessage(
                    "Integration settings lookup is not implemented for this provider yet.",
                    "integrations.settings_not_supported");
                throw new BadRequestException(response);
            }

            var typedSettings = settingsResolver.Deserialize(integration.Type, integration.Settings) as WhatsAppSettings
                                 ?? new WhatsAppSettings();

            result = new IntegrationByIdentifierResponse
            {
                Name = integration.Name,
                Provider = integration.Type,
                IsActive = integration.IsActive,
                Settings = new IntegrationSettingsResponse
                {
                    // MantÃ©m mascarado por padrÃ£o para reduzir exposiÃ§Ã£o de segredos.
                    AccessToken = "******",
                    PhoneNumberId = typedSettings.PhoneNumberId,
                    BusinessAccountId = typedSettings.BusinessAccountId,
                    WebhookVerifyToken = WhatsAppWebhookDefaults.FixedVerifyToken
                }
            };
        });

        return result ?? throw new InvalidOperationException(
            "Cross-tenant integration resolution failed to produce a result.");
    }
}


