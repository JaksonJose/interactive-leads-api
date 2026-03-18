namespace InteractiveLeads.Application.Feature.Webhooks.Messages;

public sealed class WebhookMessagePayload
{
    public string Id { get; set; } = string.Empty;

    public long Timestamp { get; set; }

    public string Direction { get; set; } = "inbound";

    public string Type { get; set; } = "text";

    public WebhookTextPayload? Text { get; set; }

    public WebhookMediaPayload? Media { get; set; }
}

public sealed class WebhookTextPayload
{
    public string Body { get; set; } = string.Empty;
}

public sealed class WebhookMediaPayload
{
    public string Url { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string? MimeType { get; set; }
    public string? Sha256 { get; set; }
    public string? FileName { get; set; }
}

