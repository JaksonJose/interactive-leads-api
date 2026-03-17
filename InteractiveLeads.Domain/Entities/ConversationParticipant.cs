using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Domain.Entities;

public class ConversationParticipant
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string? UserId { get; set; }
    public Guid? ContactId { get; set; }
    public ConversationParticipantRole Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LeftAt { get; set; }
    public bool IsActive { get; set; }

    public Conversation Conversation { get; set; } = default!;
    public Contact? Contact { get; set; }
}
