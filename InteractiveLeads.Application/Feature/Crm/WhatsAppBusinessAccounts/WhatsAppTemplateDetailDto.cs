namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

/// <summary>Template row plus content hydrated from <c>ComponentsJson</c> (CRM create shape or Meta sync shape).</summary>
public sealed class WhatsAppTemplateDetailDto
{
    public Guid Id { get; set; }

    public string MetaTemplateId { get; set; } = string.Empty;

    public DateTimeOffset LastSyncedAt { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? HeaderText { get; set; }

    public string? HeaderExample { get; set; }

    public string Body { get; set; } = string.Empty;

    public string[]? BodyExamples { get; set; }

    public string? Footer { get; set; }

    public CreateWhatsAppTemplateButtonRequest[]? Buttons { get; set; }

    public string? AuthoringHeaderText { get; set; }

    public string? AuthoringBody { get; set; }

    public WhatsAppTemplateVariableBindingDto[]? VariableBindings { get; set; }

    public bool IsMetaSynced { get; set; }

    /// <summary>Distinct Meta placeholder count (header + body).</summary>
    public int VariableSlotCount { get; set; }

    /// <summary>False when template has Meta placeholders but not all slots have a semantic binding.</summary>
    public bool VariableBindingsComplete { get; set; } = true;

    public string? SubmissionLastError { get; set; }

    public string? SubmissionLastErrorCode { get; set; }

    public DateTimeOffset? SubmissionLastErrorAt { get; set; }

    /// <summary>False when there is no Meta template id yet, or a submission error was recorded.</summary>
    public bool IsAvailableForMessaging { get; set; }
}
