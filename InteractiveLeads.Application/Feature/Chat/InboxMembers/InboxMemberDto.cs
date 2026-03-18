using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Feature.Chat.InboxMembers;

public sealed class InboxMemberDto
{
    public Guid Id { get; set; }
    public Guid InboxId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public InboxMemberRole? Role { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
}

