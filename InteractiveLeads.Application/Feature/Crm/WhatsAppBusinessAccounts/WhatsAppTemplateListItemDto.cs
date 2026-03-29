namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

public sealed class WhatsAppTemplateListItemDto
{
    public Guid Id { get; set; }

    public string MetaTemplateId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset LastSyncedAt { get; set; }

    public string? SubmissionCorrelationId { get; set; }
}
