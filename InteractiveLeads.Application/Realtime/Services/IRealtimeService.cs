namespace InteractiveLeads.Application.Realtime.Services;

public interface IRealtimeService
{
    Task SendToInboxAsync(string inboxId, object @event);
    Task SendToConversationAsync(string conversationId, object @event);
}

