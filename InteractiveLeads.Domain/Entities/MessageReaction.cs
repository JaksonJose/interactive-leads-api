namespace InteractiveLeads.Domain.Entities;

public class MessageReaction
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string Reaction { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public Message Message { get; set; } = default!;
}

