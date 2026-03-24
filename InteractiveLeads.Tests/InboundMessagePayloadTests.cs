using System.Text.Json;
using InteractiveLeads.Application.Feature.Inbound.Messages;

namespace InteractiveLeads.Tests;

public sealed class InboundMessagePayloadTests
{
    [Fact]
    public void InboundMediaPayload_deserializes_whatsapp_filename_property()
    {
        const string json = """{"url":"https://example.com/f","mimeType":"application/pdf","filename":"2024-12-10_103459.pdf"}""";
        var media = JsonSerializer.Deserialize<InboundMediaPayload>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(media);
        Assert.Equal("2024-12-10_103459.pdf", media!.FileName);
    }

    [Fact]
    public void ResolveMedia_FallsBackToTypedObject()
    {
        var payload = new InboundMessagePayload
        {
            Type = "image",
            Image = new InboundMediaPayload { Url = "https://bucket/temp/image.jpg", MimeType = "image/jpeg" }
        };

        var media = payload.ResolveMedia();

        Assert.NotNull(media);
        Assert.Equal("image/jpeg", media!.MimeType);
    }
}
