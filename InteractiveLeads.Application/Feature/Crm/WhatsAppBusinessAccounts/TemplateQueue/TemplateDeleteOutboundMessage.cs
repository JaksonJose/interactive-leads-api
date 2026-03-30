namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.TemplateQueue;

/// <summary>JSON envelope for <c>delete_template</c> (Meta deletes by template name).</summary>
public sealed class TemplateDeleteOutboundMessage
{
    public string Provider { get; set; } = "whatsapp";

    public string EventType { get; set; } = "delete_template";

    public string TenantId { get; set; } = string.Empty;

    public string WabaId { get; set; } = string.Empty;

    public TemplateCreateOutboundAuth Auth { get; set; } = new();

    public TemplateDeleteOutboundPayload Payload { get; set; } = new();

    public TemplateCreateOutboundMetadata Metadata { get; set; } = new();
}

public sealed class TemplateDeleteOutboundPayload
{
    public string Name { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;
}
