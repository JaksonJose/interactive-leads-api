namespace InteractiveLeads.Application.Realtime.Services;

public interface IRealtimeService
{
    Task SendToTenantAsync(string tenantId, object @event);
    Task SendToInboxAsync(string inboxId, object @event);
    Task SendToConversationAsync(string conversationId, object @event);

    /// <summary>
    /// Broadcasts a presence update to every active SignalR connection in the tenant group
    /// (<c>tenant:{tenantId}</c>) so all clients in that tenant receive online/offline changes.
    /// </summary>
    Task SendPresenceUpdatedToTenantAsync(string tenantId, string userId, bool isOnline, DateTimeOffset? lastSeenAtUtc);
}

