namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

/// <summary>Template row plus editable fields deserialized from <c>ComponentsJson</c>.</summary>
public sealed class WhatsAppTemplateDetailDto
{
    public Guid Id { get; set; }

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
}
