using InteractiveLeads.Application.Realtime.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace InteractiveLeads.Api.Realtime.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IRealtimeJoinAuthorizationService _joinAuthorization;

    public ChatHub(IRealtimeJoinAuthorizationService joinAuthorization)
    {
        _joinAuthorization = joinAuthorization;
    }

    public async Task JoinInbox(string inboxId)
    {
        await _joinAuthorization.EnsureCanJoinInboxAsync(inboxId, Context.ConnectionAborted);
        var inboxGuid = Guid.Parse(inboxId);

        await Groups.AddToGroupAsync(Context.ConnectionId, $"inbox:{inboxGuid}");
    }

    public async Task JoinConversation(string conversationId)
    {
        await _joinAuthorization.EnsureCanJoinConversationAsync(conversationId, Context.ConnectionAborted);
        var conversationGuid = Guid.Parse(conversationId);

        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation:{conversationGuid}");
    }

    public async Task LeaveConversation(string conversationId)
    {
        // Removing from a group doesn't require membership verification; it only affects this connection.
        var conversationGuid = Guid.Parse(conversationId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation:{conversationGuid}");
    }
}

