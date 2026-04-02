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

    /// <summary>
    /// Reassigns when the first-response SLA has expired without an agent reply, if the handling team has
    /// auto-assign and <c>AutoReassignOnFirstResponseSlaExpired</c>. Restarts SLA from UTC now.
    /// </summary>
    /// <returns>True if a new assignee was chosen.</returns>
    Task<bool> TryReassignAfterFirstResponseSlaExpiredAsync(
        Guid companyId,
        string tenantIdentifier,
        Guid conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reassigns when the customer sent the last message and it is older than the team's configured inactivity
    /// timeout minutes, after at least one agent reply was recorded. Requires auto-assign. Restarts SLA from UTC now.
    /// </summary>
    Task<bool> TryReassignAfterCustomerMessageInactivityAsync(
        Guid companyId,
        string tenantIdentifier,
        Guid conversationId,
        CancellationToken cancellationToken = default);
}
