using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Feature.Chat.InboxMembers;

public sealed class UpdateInboxMemberRequest
{
    public InboxMemberRole? Role { get; set; }
    public bool IsActive { get; set; }
}

