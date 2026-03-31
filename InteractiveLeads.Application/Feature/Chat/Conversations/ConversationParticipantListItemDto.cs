namespace InteractiveLeads.Application.Feature.Chat.Conversations;

public sealed class ConversationParticipantListItemDto
{
    public string UserId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public bool IsActive { get; set; }
}
