using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Integrations.Settings;

public interface IIntegrationSettingsResolver
{
    object Deserialize(IntegrationType provider, string? json);

    string Serialize(IntegrationType provider, object settings);
}

