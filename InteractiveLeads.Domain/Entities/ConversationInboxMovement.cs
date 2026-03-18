namespace InteractiveLeads.Domain.Entities;

public class ConversationInboxMovement
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid FromInboxId { get; set; }
    public Guid ToInboxId { get; set; }
    public string MovedByUserId { get; set; } = string.Empty;
    public DateTimeOffset MovedAt { get; set; }
    public string? Reason { get; set; }

    public Conversation Conversation { get; set; } = default!;
}

