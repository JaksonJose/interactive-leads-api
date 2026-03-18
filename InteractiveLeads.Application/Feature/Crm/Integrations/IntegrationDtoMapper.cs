using InteractiveLeads.Application.Integrations.Settings;
using InteractiveLeads.Domain.Entities;

namespace InteractiveLeads.Application.Feature.Crm.Integrations;

public static class IntegrationDtoMapper
{
    public static IntegrationResponse ToResponse(
        Integration integration,
        IIntegrationSettingsResolver settingsResolver)
    {
        object? maskedSettings = null;

        if (!string.IsNullOrWhiteSpace(integration.Settings))
        {
            var typedSettings = settingsResolver.Deserialize(integration.Type, integration.Settings);

            maskedSettings = typedSettings switch
            {
                WhatsAppSettings whatsapp => new
                {
                    accessToken = "******",
                    phoneNumberId = whatsapp.PhoneNumberId,
                    businessAccountId = whatsapp.BusinessAccountId,
                    webhookVerifyToken = whatsapp.WebhookVerifyToken
                },
                _ => null
            };
        }

        return new IntegrationResponse
        {
            Id = integration.Id,
            Name = integration.Name,
            Provider = integration.Type,
            IsActive = integration.IsActive,
            Settings = maskedSettings
        };
    }
}

