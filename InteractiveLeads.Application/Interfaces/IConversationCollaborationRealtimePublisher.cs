using InteractiveLeads.Domain.Entities;

namespace InteractiveLeads.Application.Interfaces;

public interface IConversationCollaborationRealtimePublisher
{
    Task PublishCollaborationUpdatedAsync(Conversation conversation, CancellationToken cancellationToken);
}
