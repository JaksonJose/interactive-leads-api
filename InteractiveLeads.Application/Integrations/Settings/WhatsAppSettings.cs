using System.Text.Json.Serialization;

namespace InteractiveLeads.Application.Integrations.Settings;

public sealed class WhatsAppSettings
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumberId")]
    public string PhoneNumberId { get; set; } = string.Empty;

    [JsonPropertyName("businessAccountId")]
    public string BusinessAccountId { get; set; } = string.Empty;

    [JsonPropertyName("webhookVerifyToken")]
    public string WebhookVerifyToken { get; set; } = string.Empty;

    [JsonPropertyName("inboxId")]
    public Guid? InboxId { get; set; }
}

