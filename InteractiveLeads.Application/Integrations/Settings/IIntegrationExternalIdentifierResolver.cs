using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Integrations.Settings;

public interface IIntegrationExternalIdentifierResolver
{
    string ResolveExternalIdentifier(IntegrationType provider, object settings);
}

