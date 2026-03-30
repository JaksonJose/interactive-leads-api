namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.TemplateQueue;

/// <summary>JSON envelope for <c>template_synced</c> (n8n syncs templates from Meta into the app DB).</summary>
public sealed class TemplateSyncedOutboundMessage
{
    public string Provider { get; set; } = "whatsapp";

    public string EventType { get; set; } = "template_synced";

    public TemplateSyncedOutboundIdentifications Identifications { get; set; } = new();

    /// <summary>Credentials for Graph calls in the worker (same shape as create/delete).</summary>
    public TemplateCreateOutboundAuth Auth { get; set; } = new();

    public TemplateSyncedOutboundPayload Payload { get; set; } = new();
}

public sealed class TemplateSyncedOutboundIdentifications
{
    public string TenantId { get; set; } = string.Empty;

    public string WabaId { get; set; } = string.Empty;

    public string IntegrationId { get; set; } = string.Empty;

    public string WhatsAppBusinessAccountId { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class TemplateSyncedOutboundPayload
{
    /// <summary>When true, consumer should list all templates for the WABA and upsert locally.</summary>
    public bool SyncAll { get; set; } = true;
}
