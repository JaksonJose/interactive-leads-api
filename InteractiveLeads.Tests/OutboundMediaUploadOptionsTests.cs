using InteractiveLeads.Application.Feature.Chat.Media;

namespace InteractiveLeads.Tests;

public sealed class OutboundMediaUploadOptionsTests
{
    [Fact]
    public void Defaults_match_whatsapp_cloud_caps_for_image_and_video()
    {
        var o = new OutboundMediaUploadOptions();
        Assert.Equal(5L * 1024 * 1024, o.MaxImageBytes);
        Assert.Equal(16L * 1024 * 1024, o.MaxVideoBytes);
        Assert.Equal(16L * 1024 * 1024, o.MaxAudioBytes);
        Assert.Equal(100L * 1024 * 1024, o.MaxDocumentBytes);
        Assert.Contains("video/mp4", o.AllowedVideoMimeTypes);
        Assert.Contains("video/3gpp", o.AllowedVideoMimeTypes);
        Assert.Contains("image/jpeg", o.AllowedImageMimeTypes);
        Assert.DoesNotContain("video/quicktime", o.AllowedVideoMimeTypes);
        Assert.DoesNotContain("video/webm", o.AllowedVideoMimeTypes);
    }
}
