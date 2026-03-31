using InteractiveLeads.Application.Realtime.Services;
using InteractiveLeads.Application.Realtime.Services.Presence;
using InteractiveLeads.Infrastructure.Tenancy.Strategies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace InteractiveLeads.Api.Realtime.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IRealtimeJoinAuthorizationService _joinAuthorization;
    private readonly IPresenceService _presence;
    private readonly IRealtimeService _realtime;

    public ChatHub(IRealtimeJoinAuthorizationService joinAuthorization, IPresenceService presence, IRealtimeService realtime)
    {
        _joinAuthorization = joinAuthorization;
        _presence = presence;
        _realtime = realtime;
    }

    public override async Task OnConnectedAsync()
    {
        var (tenantId, userId) = GetTenantAndUser();
        if (!string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
            try
            {
                var state = await _presence.ConnectionOpenedAsync(tenantId!, userId!, Context.ConnectionId, Context.ConnectionAborted);
                await _realtime.SendToTenantAsync(state.TenantId, new
                {
                    type = "presence.updated",
                    tenantId = state.TenantId,
                    timestamp = DateTimeOffset.UtcNow.ToString("O"),
                    payload = new { userId = state.UserId, isOnline = state.IsOnline, lastSeenAtUtc = state.LastSeenAtUtc }
                });
            }
            catch (OperationCanceledException)
            {
                // Refresh/rapid reconnect can cancel the handshake; presence is best-effort.
            }
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            // IMPORTANT: On refresh the connection token is already cancelled.
            // Presence cleanup should still run, so avoid Context.ConnectionAborted here.
            var state = await _presence.ConnectionClosedAsync(Context.ConnectionId, CancellationToken.None);
            if (state is not null)
            {
                await _realtime.SendToTenantAsync(state.TenantId, new
                {
                    type = "presence.updated",
                    tenantId = state.TenantId,
                    timestamp = DateTimeOffset.UtcNow.ToString("O"),
                    payload = new { userId = state.UserId, isOnline = state.IsOnline, lastSeenAtUtc = state.LastSeenAtUtc }
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Disconnect path can be cancelled by the server host; ignore.
        }
        await base.OnDisconnectedAsync(exception);
    }

    private (string? tenantId, string? userId) GetTenantAndUser()
    {
        var http = Context.GetHttpContext();
        var tenant = http?.Request.Headers["tenant"].ToString();
        if (string.IsNullOrWhiteSpace(tenant))
        {
            // HTTP uses header; WebSocket reconnect may not resend custom headers — use same claims as TokenService / PresenceController.
            tenant = Context.User?.FindFirst("tenant")?.Value
                ?? Context.User?.FindFirst("tenantId")?.Value
                ?? Context.User?.FindFirst(JwtTenantFallbackStrategy.TenantIdClaimType)?.Value;
        }
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User?.FindFirst("sub")?.Value;
        return (tenant, userId);
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

