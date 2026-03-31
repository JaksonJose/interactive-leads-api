namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

/// <summary>Delete request: Meta identifies templates by name (and language in the worker).</summary>
public sealed class DeleteWhatsAppTemplateRequest
{
    /// <summary>Exact template name as registered (must match the row).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// When true, bypass the "disable + wait for worker reply" flow and remove the row immediately after publishing.
    /// This is mainly for operational scenarios (e.g. cleanup) when async delete callbacks are unavailable.
    /// </summary>
    public bool DeleteImmediatelyFromDatabase { get; set; }
}
