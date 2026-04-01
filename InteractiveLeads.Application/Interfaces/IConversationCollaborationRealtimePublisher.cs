using InteractiveLeads.Domain.Entities;

namespace InteractiveLeads.Application.Interfaces;

public interface IConversationCollaborationRealtimePublisher
{
    Task PublishCollaborationUpdatedAsync(Conversation conversation, CancellationToken cancellationToken);

    /// <summary>Used when there is no interactive user context (e.g. inbound system assignment).</summary>
    Task PublishCollaborationUpdatedAsync(Conversation conversation, string tenantIdentifier, CancellationToken cancellationToken);
}
