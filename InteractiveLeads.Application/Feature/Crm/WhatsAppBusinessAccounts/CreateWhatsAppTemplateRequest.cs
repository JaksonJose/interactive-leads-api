namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

public sealed class CreateWhatsAppTemplateButtonRequest
{
    public string Type { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string? PhoneNumber { get; set; }
}

public sealed class CreateWhatsAppTemplateRequest
{
    public string Name { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string? HeaderText { get; set; }

    public string? HeaderExample { get; set; }

    public string Body { get; set; } = string.Empty;

    public string[]? BodyExamples { get; set; }

    public string? Footer { get; set; }

    public CreateWhatsAppTemplateButtonRequest[]? Buttons { get; set; }
}

public sealed class CreateWhatsAppTemplateAcceptedDto
{
    public string CorrelationId { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
