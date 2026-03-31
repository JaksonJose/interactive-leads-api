namespace InteractiveLeads.Domain.Entities;

/// <summary>Cached row for a WhatsApp message template (Meta scope: WABA). Sync job/API fills this; sending uses name + language against the parent WABA.</summary>
public class WhatsAppTemplate
{
    public Guid Id { get; set; }

    /// <summary>Soft disable flag (CRM only). Disabled templates should not be used for messaging.</summary>
    public bool IsDisabled { get; set; }

    public DateTimeOffset? DisabledAt { get; set; }

    /// <summary>Optional reason for disable (e.g. "user_disabled", "delete_requested", "delete_failed").</summary>
    public string? DisabledReason { get; set; }

    /// <summary>Delete requested and waiting for worker reply; on success the row is removed from the DB.</summary>
    public bool DeletePending { get; set; }

    public DateTimeOffset? DeleteRequestedAt { get; set; }

    public string? DeleteLastError { get; set; }

    public string? DeleteLastErrorCode { get; set; }

    public DateTimeOffset? DeleteLastErrorAt { get; set; }

    /// <summary>Template id from Meta Graph API when available.</summary>
    public string MetaTemplateId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    /// <summary>Last error message from the template outbound worker (Meta create failed); cleared when <see cref="MetaTemplateId"/> is set.</summary>
    public string? SubmissionLastError { get; set; }

    /// <summary>Optional provider or internal error code (e.g. Meta <c>error_subcode</c>).</summary>
    public string? SubmissionLastErrorCode { get; set; }

    /// <summary>When <see cref="SubmissionLastError"/> was recorded.</summary>
    public DateTimeOffset? SubmissionLastErrorAt { get; set; }

    public Guid WhatsAppBusinessAccountId { get; set; }

    public WhatsAppBusinessAccount WhatsAppBusinessAccount { get; set; } = default!;

    /// <summary>Serialized template components (structure from Meta).</summary>
    public string ComponentsJson { get; set; } = "{}";

    public DateTimeOffset LastSyncedAt { get; set; }
}
