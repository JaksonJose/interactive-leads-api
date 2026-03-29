namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.TemplateQueue;

/// <summary>JSON envelope published to the template outbound queue (<c>create_template</c>).</summary>
public sealed class TemplateCreateOutboundMessage
{
    public string Provider { get; set; } = "whatsapp";

    public string EventType { get; set; } = "create_template";

    public string TenantId { get; set; } = string.Empty;

    public string WabaId { get; set; } = string.Empty;

    public TemplateCreateOutboundAuth Auth { get; set; } = new();

    public TemplateCreateOutboundPayload Payload { get; set; } = new();

    public TemplateCreateOutboundMetadata Metadata { get; set; } = new();
}

public sealed class TemplateCreateOutboundAuth
{
    public string AccessToken { get; set; } = string.Empty;

    public string PhoneNumberId { get; set; } = string.Empty;

    public string BusinessAccountId { get; set; } = string.Empty;
}

public sealed class TemplateCreateOutboundPayload
{
    public string Name { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    /// <summary>Meta Graph <c>components</c> array (serialized as JSON).</summary>
    public List<Dictionary<string, object?>> Components { get; set; } = [];
}

public sealed class TemplateCreateOutboundMetadata
{
    public string CorrelationId { get; set; } = string.Empty;

    public string IntegrationId { get; set; } = string.Empty;

    public string WhatsAppBusinessAccountId { get; set; } = string.Empty;
}
