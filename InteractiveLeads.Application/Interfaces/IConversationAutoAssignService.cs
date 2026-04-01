using InteractiveLeads.Domain.Entities;

namespace InteractiveLeads.Application.Interfaces;

/// <summary>Routes a new conversation through inbox team priority and assigns an agent when team auto-assign is enabled.</summary>
public interface IConversationAutoAssignService
{
    Task TryAssignNewConversationAsync(
        Guid companyId,
        string tenantIdentifier,
        Conversation conversation,
        CancellationToken cancellationToken);
}
