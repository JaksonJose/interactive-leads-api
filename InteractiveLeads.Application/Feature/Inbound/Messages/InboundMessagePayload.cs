using System.Text.Json.Serialization;

namespace InteractiveLeads.Application.Feature.Inbound.Messages;

public sealed class InboundMessagePayload
{
    public string Id { get; set; } = string.Empty;

    /// <summary>Internal API message id (<c>Message.Id</c>) echoed from outbound <c>payload.clientMessageId</c> for send confirmation.</summary>
    public string? ClientMessageId { get; set; }

    public long Timestamp { get; set; }

    public string Direction { get; set; } = "inbound";

    public string Type { get; set; } = "text";

    public InboundTextPayload? Text { get; set; }

    public InboundMediaPayload? Media { get; set; }

    public InboundMediaPayload? Image { get; set; }
    public InboundMediaPayload? Video { get; set; }
    public InboundMediaPayload? Audio { get; set; }
    public InboundMediaPayload? Document { get; set; }
    public InboundMediaPayload? Sticker { get; set; }

    public InboundMediaPayload? ResolveMedia()
    {
        if (Media is not null)
            return Media;

        return Type.Trim().ToLowerInvariant() switch
        {
            "image" => Image,
            "video" => Video,
            "audio" => Audio,
            "document" => Document,
            "sticker" => Sticker,
            _ => null
        };
    }
}

public sealed class InboundTextPayload
{
    public string Body { get; set; } = string.Empty;
}

public sealed class InboundMediaPayload
{
    public string Url { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string? MimeType { get; set; }
    public string? Sha256 { get; set; }

    [JsonPropertyName("filename")]
    public string? FileName { get; set; }
}
