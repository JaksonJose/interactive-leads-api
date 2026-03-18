using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Integrations.Settings;

public sealed class IntegrationExternalIdentifierResolver : IIntegrationExternalIdentifierResolver
{
    public string ResolveExternalIdentifier(IntegrationType provider, object settings)
    {
        return provider switch
        {
            IntegrationType.WhatsApp => ResolveWhatsAppExternalIdentifier(settings),
            _ => ThrowUnsupportedProvider(provider)
        };
    }

    private static string ResolveWhatsAppExternalIdentifier(object settings)
    {
        if (settings is not WhatsAppSettings whatsAppSettings)
        {
            var response = new Responses.ResultResponse();
            response.AddErrorMessage("Invalid settings type for WhatsApp integration.", "integrations.invalid_settings_type");
            throw new BadRequestException(response);
        }

        var phoneNumberId = (whatsAppSettings.PhoneNumberId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(phoneNumberId))
        {
            var response = new Responses.ResultResponse();
            response.AddErrorMessage("Phone number id is required for WhatsApp integrations.", "integrations.whatsapp.phone_number_id_required");
            throw new BadRequestException(response);
        }

        return phoneNumberId;
    }

    private static string ThrowUnsupportedProvider(IntegrationType provider)
    {
        var response = new Responses.ResultResponse();
        response.AddErrorMessage("Unsupported integration provider.", "integrations.unsupported_provider");
        throw new BadRequestException(response);
    }
}

