using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Feature.Chat.InboxMembers;

public sealed class InboxMemberDto
{
    public Guid Id { get; set; }
    public Guid InboxId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserDisplayName { get; set; }
    public string? UserEmail { get; set; }
    public InboxMemberRole? Role { get; set; }
    public bool IsActive { get; set; }
    public bool CanBeAssigned { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
}

