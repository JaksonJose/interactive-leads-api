using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Feature.Chat.Conversations;

public sealed class InboxConversationListItemDto
{
    public Guid Id { get; set; }
    public Guid InboxId { get; set; }
    public Guid ContactId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTimeOffset LastMessageAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string InboxName { get; set; } = string.Empty;
    public ConversationStatus Status { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public string? AssignedAgentName { get; set; }
}

