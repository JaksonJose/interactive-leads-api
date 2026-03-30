namespace InteractiveLeads.Domain.Entities;

/// <summary>Cached row for a WhatsApp message template (Meta scope: WABA). Sync job/API fills this; sending uses name + language against the parent WABA.</summary>
public class WhatsAppTemplate
{
    public Guid Id { get; set; }

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
