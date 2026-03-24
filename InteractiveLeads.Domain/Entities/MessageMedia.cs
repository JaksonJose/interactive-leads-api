using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Domain.Entities;

public class MessageMedia
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public MediaType MediaType { get; set; }
    public string Url { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    /// <summary>Original file name when provided by the channel (e.g. WhatsApp <c>filename</c>).</summary>
    public string? FileName { get; set; }
    public string? Caption { get; set; }

    public Message Message { get; set; } = default!;
}

