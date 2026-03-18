using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Feature.Chat.Conversations;

public sealed class ConversationDto
{
    public Guid Id { get; set; }
    public Guid InboxId { get; set; }
    public Guid ContactId { get; set; }
    public ConversationStatus Status { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public DateTimeOffset LastMessageAt { get; set; }
    public int Priority { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

