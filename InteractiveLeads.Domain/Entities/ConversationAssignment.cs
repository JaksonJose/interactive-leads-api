namespace InteractiveLeads.Domain.Entities;

public class ConversationAssignment
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? AssignedBy { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset? UnassignedAt { get; set; }
    public string? Reason { get; set; }

    public Conversation Conversation { get; set; } = default!;
}
