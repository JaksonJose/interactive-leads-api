using System.Security.Cryptography;
using InteractiveLeads.Application.Interfaces;

namespace InteractiveLeads.Infrastructure.Media;

public sealed class MediaContentInspector : IMediaContentInspector
{
    public async Task<string> ComputeSha256Async(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
            stream.Position = 0;

        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);

        if (stream.CanSeek)
            stream.Position = 0;

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
