namespace InteractiveLeads.Application.Feature.Chat.Media;

public sealed class UploadConversationMediaRequest
{
    public Guid ConversationId { get; set; }
    public Stream Content { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long ContentLength { get; set; }
    /// <summary>Optional folder hint: image, document, or audio. Required when MIME is ambiguous.</summary>
    public string? MediaType { get; set; }
}
