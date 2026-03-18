using System.Text.Json;
using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Feature.Crm.Integrations;

public sealed class CreateIntegrationRequest
{
    public string Name { get; set; } = string.Empty;

    public IntegrationType Provider { get; set; }

    public JsonElement Settings { get; set; }

    public bool IsActive { get; set; } = true;
}

