using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Feature.Chat.InboxMembers;

public sealed class AddInboxMemberRequest
{
    public string UserId { get; set; } = string.Empty;
    public InboxMemberRole? Role { get; set; }
    public bool IsActive { get; set; } = true;
}

