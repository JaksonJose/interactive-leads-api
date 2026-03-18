using System.Text.Json;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Integrations.Settings;

public sealed class IntegrationSettingsResolver : IIntegrationSettingsResolver
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public object Deserialize(IntegrationType provider, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return provider switch
            {
                IntegrationType.WhatsApp => new WhatsAppSettings(),
                _ => throw CreateUnsupportedProviderException(provider)
            };
        }

        return provider switch
        {
            IntegrationType.WhatsApp => JsonSerializer.Deserialize<WhatsAppSettings>(json, SerializerOptions)
                ?? new WhatsAppSettings(),
            _ => throw CreateUnsupportedProviderException(provider)
        };
    }

    public string Serialize(IntegrationType provider, object settings)
    {
        return provider switch
        {
            IntegrationType.WhatsApp => JsonSerializer.Serialize(
                (WhatsAppSettings)settings,
                SerializerOptions),
            _ => throw CreateUnsupportedProviderException(provider)
        };
    }

    private static Exception CreateUnsupportedProviderException(IntegrationType provider)
    {
        var response = new Responses.ResultResponse();
        response.AddErrorMessage("Unsupported integration provider.", "integrations.unsupported_provider");
        return new BadRequestException(response);
    }
}

