using InteractiveLeads.Application.Feature.Inbound.Messages;

namespace InteractiveLeads.Tests;

public sealed class InboundMessagePayloadTests
{
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
