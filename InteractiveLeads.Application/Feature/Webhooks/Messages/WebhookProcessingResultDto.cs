namespace InteractiveLeads.Application.Feature.Webhooks.Messages;

public sealed class WebhookProcessingResultDto
{
    public bool Processed { get; set; }

    public string? Reason { get; set; }
}

