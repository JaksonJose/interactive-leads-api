using InteractiveLeads.Infrastructure.Media;

namespace InteractiveLeads.Tests;

public sealed class MediaContentInspectorTests
{
    [Fact]
    public async Task ComputeSha256Async_ReturnsStableHash()
    {
        var inspector = new MediaContentInspector();
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("interactive-media"));

        var hash1 = await inspector.ComputeSha256Async(stream, CancellationToken.None);
        var hash2 = await inspector.ComputeSha256Async(stream, CancellationToken.None);

        Assert.Equal(hash1, hash2);
        Assert.False(string.IsNullOrWhiteSpace(hash1));
    }
}
