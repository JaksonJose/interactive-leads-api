namespace InteractiveLeads.Application.Realtime.Services.Presence;

public record PresenceStateDto(
    string TenantId,
    string UserId,
    bool IsOnline,
    DateTimeOffset? LastSeenAtUtc);

public interface IPresenceService
{
    /// <summary>Marks a connection as active for a given user, returning the updated state.</summary>
    Task<PresenceStateDto> ConnectionOpenedAsync(string tenantId, string userId, string connectionId, CancellationToken ct);

    /// <summary>Marks a connection as closed, returning the updated state if resolvable.</summary>
    Task<PresenceStateDto?> ConnectionClosedAsync(string connectionId, CancellationToken ct);

    /// <summary>Snapshot of presence states for a tenant.</summary>
    Task<IReadOnlyList<PresenceStateDto>> ListTenantPresenceAsync(string tenantId, CancellationToken ct);

    /// <summary>
    /// Renews session TTL for this SignalR connection and prunes stale sibling connection fields for the same user.
    /// Returns null when the session is valid or unknown (no broadcast). Reserved for future offline events from the heartbeat path.
    /// </summary>
    Task<PresenceStateDto?> HeartbeatAsync(
        string connectionId,
        string tenantId,
        string userId,
        CancellationToken ct);
}

