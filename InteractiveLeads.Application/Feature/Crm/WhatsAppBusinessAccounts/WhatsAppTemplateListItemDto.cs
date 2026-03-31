namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

public sealed class WhatsAppTemplateListItemDto
{
    public Guid Id { get; set; }

    public bool IsDisabled { get; set; }

    public DateTimeOffset? DisabledAt { get; set; }

    public string? DisabledReason { get; set; }

    public bool DeletePending { get; set; }

    public DateTimeOffset? DeleteRequestedAt { get; set; }

    public string? DeleteLastError { get; set; }

    public string? DeleteLastErrorCode { get; set; }

    public DateTimeOffset? DeleteLastErrorAt { get; set; }

    public string MetaTemplateId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset LastSyncedAt { get; set; }

    public string? SubmissionCorrelationId { get; set; }

    /// <summary>Number of Meta placeholders ({{n}}) in header+body.</summary>
    public int VariableSlotCount { get; set; }

    /// <summary>All slots mapped to CRM fields (or no placeholders).</summary>
    public bool VariableBindingsComplete { get; set; } = true;

    public string? SubmissionLastError { get; set; }

    public string? SubmissionLastErrorCode { get; set; }

    public DateTimeOffset? SubmissionLastErrorAt { get; set; }

    /// <summary>False when there is no Meta template id yet, or a submission error was recorded.</summary>
    public bool IsAvailableForMessaging { get; set; }
}
