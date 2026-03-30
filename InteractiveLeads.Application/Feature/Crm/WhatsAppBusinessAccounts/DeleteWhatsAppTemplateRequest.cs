namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

/// <summary>Delete request: Meta identifies templates by name (and language in the worker).</summary>
public sealed class DeleteWhatsAppTemplateRequest
{
    /// <summary>Exact template name as registered (must match the row).</summary>
    public string Name { get; set; } = string.Empty;
}
