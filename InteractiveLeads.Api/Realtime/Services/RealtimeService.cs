using InteractiveLeads.Application.Realtime.Services;
using InteractiveLeads.Api.Realtime.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace InteractiveLeads.Api.Realtime.Services;

public class RealtimeService : IRealtimeService
{
    private readonly IHubContext<ChatHub> _hubContext;

    public RealtimeService(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task SendPresenceUpdatedToTenantAsync(string tenantId, string userId, bool isOnline, DateTimeOffset? lastSeenAtUtc)
    {
        return _hubContext.Clients.Group($"tenant:{tenantId}").SendAsync("event", new
        {
            type = "presence.updated",
            tenantId,
            timestamp = DateTimeOffset.UtcNow.ToString("O"),
            payload = new { userId, isOnline, lastSeenAtUtc }
        });
    }

    public Task SendToTenantAsync(string tenantId, object @event)
    {
        return _hubContext.Clients.Group($"tenant:{tenantId}").SendAsync("event", @event);
    }

    public Task SendToInboxAsync(string inboxId, object @event)
    {
        var inboxGuid = Guid.Parse(inboxId);
        return _hubContext.Clients.Group($"inbox:{inboxGuid}").SendAsync("event", @event);
    }

    public Task SendToConversationAsync(string conversationId, object @event)
    {
        var conversationGuid = Guid.Parse(conversationId);
        return _hubContext.Clients.Group($"conversation:{conversationGuid}").SendAsync("event", @event);
    }
}

