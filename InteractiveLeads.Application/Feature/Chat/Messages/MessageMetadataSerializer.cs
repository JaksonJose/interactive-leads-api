using System.Text.Json;

namespace InteractiveLeads.Application.Feature.Chat.Messages;

public static class MessageMetadataSerializer
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static MessageTemplateSnapshotDto? TryReadTemplateSnapshot(string? metadata, string? messageType)
    {
        if (!string.Equals(messageType, "template", StringComparison.OrdinalIgnoreCase))
            return null;
        if (string.IsNullOrWhiteSpace(metadata))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(metadata);
            if (!doc.RootElement.TryGetProperty("templateSnapshot", out var el) || el.ValueKind == JsonValueKind.Null)
                return null;
            return el.Deserialize<MessageTemplateSnapshotDto>(ReadOptions);
        }
        catch
        {
            return null;
        }
    }
}
