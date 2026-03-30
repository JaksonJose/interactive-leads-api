namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

/// <summary>Whether a CRM template row can be used for WhatsApp Cloud API sends (requires Meta template id; no recorded submission failure).</summary>
public static class WhatsAppTemplateMessagingRules
{
    public static bool IsAvailableForMessaging(string? metaTemplateId, DateTimeOffset? submissionLastErrorAt) =>
        !string.IsNullOrWhiteSpace(metaTemplateId) && submissionLastErrorAt is null;
}
