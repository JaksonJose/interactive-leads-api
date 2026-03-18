using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Domain.Entities;

public class InboxMember
{
    public Guid Id { get; set; }
    public Guid InboxId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public InboxMemberRole? Role { get; set; }
    public bool IsActive { get; set; }
    public bool CanBeAssigned { get; set; } = true;
    public DateTimeOffset JoinedAt { get; set; }

    public Inbox Inbox { get; set; } = default!;
}
