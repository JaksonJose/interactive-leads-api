using InteractiveLeads.Application.Feature.Inbound.Media;

namespace InteractiveLeads.Infrastructure.Media;

internal static class MediaOriginalKeyExtensions
{
    /// <summary>Suffix after "original", e.g. ".ogg" or ".mp4" (includes the dot).</summary>
    public static string GetOriginalExtension(ProcessMediaRequest request)
    {
        var ext = Path.GetExtension(request.OriginalFileName ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(ext))
            return ext;

        var mime = request.MimeType?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(mime))
        {
            var semicolon = mime.IndexOf(';');
            if (semicolon >= 0)
                mime = mime[..semicolon].TrimEnd();
        }

        return mime switch
        {
            "video/mp4" => ".mp4",
            "audio/mpeg" => ".mp3",
            "audio/mp4" => ".m4a",
            "audio/ogg" => ".ogg",
            "audio/opus" => ".opus",
            _ => ".bin"
        };
    }
}
