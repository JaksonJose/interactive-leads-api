namespace InteractiveLeads.Application.Realtime.Services;

public interface IRealtimeJoinAuthorizationService
{
    Task EnsureCanJoinInboxAsync(string inboxId, CancellationToken cancellationToken);
    Task EnsureCanJoinConversationAsync(string conversationId, CancellationToken cancellationToken);
}

