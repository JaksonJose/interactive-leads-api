namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.TemplateQueue;

/// <summary>JSON envelope published to the template outbound queue (<c>update_template</c>).</summary>
public sealed class TemplateUpdateOutboundMessage
{
    public string Provider { get; set; } = "whatsapp";

    public string EventType { get; set; } = "update_template";

    public string TenantId { get; set; } = string.Empty;

    public string WabaId { get; set; } = string.Empty;

    public TemplateCreateOutboundAuth Auth { get; set; } = new();

    public TemplateUpdateOutboundPayload Payload { get; set; } = new();

    public TemplateCreateOutboundMetadata Metadata { get; set; } = new();
}

public sealed class TemplateUpdateOutboundPayload
{
    /// <summary>Meta template id (HSM id) to PATCH/update on WhatsApp.</summary>
    public string MetaTemplateId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    /// <summary>Meta Graph <c>components</c> array (serialized as JSON).</summary>
    public List<Dictionary<string, object?>> Components { get; set; } = [];
}
