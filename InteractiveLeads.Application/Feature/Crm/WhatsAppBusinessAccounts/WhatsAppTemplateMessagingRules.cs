namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

/// <summary>
/// Whether a CRM template row can be used for WhatsApp Cloud API sends.
/// Requires a Meta template id, no recorded submission failure, and Meta <c>status</c> usable for delivery (typically <c>APPROVED</c>).
/// </summary>
public static class WhatsAppTemplateMessagingRules
{
    public static bool IsAvailableForMessaging(
        string? metaTemplateId,
        DateTimeOffset? submissionLastErrorAt,
        string? status)
    {
        if (string.IsNullOrWhiteSpace(metaTemplateId) || submissionLastErrorAt is not null)
            return false;

        // Meta returns uppercase (e.g. PENDING, APPROVED). Only approved templates can be sent in production.
        return string.Equals(status?.Trim(), "APPROVED", StringComparison.OrdinalIgnoreCase);
    }
}
