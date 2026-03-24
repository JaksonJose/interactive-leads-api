namespace InteractiveLeads.Application.Feature.Chat.Media;

/// <summary>JPEG or PNG bytes produced for WhatsApp image delivery.</summary>
public sealed class OutboundWhatsAppImageEncodingResult : IAsyncDisposable, IDisposable
{
    public OutboundWhatsAppImageEncodingResult(MemoryStream stream, string contentType, string fileExtension)
    {
        Stream = stream;
        ContentType = contentType;
        FileExtension = fileExtension.StartsWith('.') ? fileExtension : "." + fileExtension;
    }

    public MemoryStream Stream { get; }
    public string ContentType { get; }
    /// <summary>Includes leading dot, e.g. <c>.jpg</c> or <c>.png</c>.</summary>
    public string FileExtension { get; }

    public ValueTask DisposeAsync() => Stream.DisposeAsync();

    public void Dispose() => Stream.Dispose();
}
