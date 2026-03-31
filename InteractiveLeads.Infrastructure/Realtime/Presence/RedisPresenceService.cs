using System.Globalization;
using InteractiveLeads.Application.Realtime.Services.Presence;
using StackExchange.Redis;

namespace InteractiveLeads.Infrastructure.Realtime.Presence;

public sealed class RedisPresenceService(IConnectionMultiplexer mux) : IPresenceService
{
    private readonly IDatabase _db = mux.GetDatabase();

    private static RedisKey UserKey(string tenantId, string userId) => $"presence:user:{tenantId}:{userId}";
    private static RedisKey ConnKey(string connectionId) => $"presence:conn:{connectionId}";
    private static RedisValue UserConnField(string connectionId) => connectionId;

    public async Task<PresenceStateDto> ConnectionOpenedAsync(
        string tenantId,
        string userId,
        string connectionId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Map connectionId -> (tenantId,userId) for disconnect cleanup.
        var connMap = new HashEntry[]
        {
            new("tenantId", tenantId),
            new("userId", userId),
        };

        // Use a set-like map of active connections in the user hash to avoid double-counting.
        var userKey = UserKey(tenantId, userId);
        var tran = _db.CreateTransaction();
        _ = tran.HashSetAsync(ConnKey(connectionId), connMap);
        _ = tran.HashSetAsync(userKey, new[] { new HashEntry(UserConnField(connectionId), "1") });
        // StackExchange.Redis rejects RedisValue.Null in HSET; dropping the field matches "online, no last-seen stamp".
        _ = tran.HashDeleteAsync(userKey, "lastSeenAtUtc");

        var exec = await tran.ExecuteAsync();
        if (!exec)
        {
            // Best-effort fallback: still report online.
            return new PresenceStateDto(tenantId, userId, true, null);
        }

        var count = await CountConnectionsAsync(userKey);
        return new PresenceStateDto(tenantId, userId, count > 0, null);
    }

    public async Task<PresenceStateDto?> ConnectionClosedAsync(string connectionId, CancellationToken ct)
    {
        // Disconnect cleanup should run even if the caller token is cancelled (e.g. browser refresh).

        var connKey = ConnKey(connectionId);
        var map = await _db.HashGetAllAsync(connKey);
        if (map is null || map.Length == 0)
        {
            // Nothing to cleanup.
            return null;
        }

        string? tenantId = null;
        string? userId = null;
        foreach (var e in map)
        {
            if (e.Name == "tenantId") tenantId = e.Value.ToString();
            else if (e.Name == "userId") userId = e.Value.ToString();
        }

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(userId))
        {
            await _db.KeyDeleteAsync(connKey);
            return null;
        }

        var userKey = UserKey(tenantId!, userId!);
        var tran = _db.CreateTransaction();
        _ = tran.HashDeleteAsync(userKey, new RedisValue[] { UserConnField(connectionId) });
        _ = tran.KeyDeleteAsync(connKey);
        var exec = await tran.ExecuteAsync();
        if (!exec)
        {
            return new PresenceStateDto(tenantId!, userId!, false, DateTimeOffset.UtcNow);
        }

        var count = await CountConnectionsAsync(userKey);
        var online = count > 0;
        DateTimeOffset? lastSeen = null;
        if (!online)
        {
            lastSeen = DateTimeOffset.UtcNow;
            await _db.HashSetAsync(userKey, new[] { new HashEntry("lastSeenAtUtc", lastSeen.Value.ToString("O", CultureInfo.InvariantCulture)) });
        }

        return new PresenceStateDto(tenantId!, userId!, online, lastSeen);
    }

    public async Task<IReadOnlyList<PresenceStateDto>> ListTenantPresenceAsync(string tenantId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // MVP: scan keys by prefix. For larger tenants, replace with a tenant index set.
        var server = muxServer();
        var keys = server.Keys(pattern: $"presence:user:{tenantId}:*").ToArray();

        var list = new List<PresenceStateDto>(keys.Length);
        foreach (var k in keys)
        {
            ct.ThrowIfCancellationRequested();
            var parts = k.ToString().Split(':');
            if (parts.Length < 4) continue;
            var userId = parts[^1];
            var entries = await _db.HashGetAllAsync(k);
            var connCount = entries.Count(e => e.Name.ToString() != "lastSeenAtUtc" && !e.Name.ToString().StartsWith("meta:", StringComparison.Ordinal));
            DateTimeOffset? lastSeen = null;
            var lastSeenRaw = entries.FirstOrDefault(e => e.Name == "lastSeenAtUtc").Value;
            if (lastSeenRaw.HasValue && DateTimeOffset.TryParse(lastSeenRaw.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                lastSeen = dt;
            list.Add(new PresenceStateDto(tenantId, userId, connCount > 0, lastSeen));
        }
        return list;

        IServer muxServer()
        {
            var ep = mux.GetEndPoints().First();
            return mux.GetServer(ep);
        }
    }

    private async Task<int> CountConnectionsAsync(RedisKey userKey)
    {
        var entries = await _db.HashGetAllAsync(userKey);
        var count = 0;
        foreach (var e in entries)
        {
            var n = e.Name.ToString();
            if (n == "lastSeenAtUtc") continue;
            if (n == "") continue;
            // connectionId fields are stored directly as field name.
            if (n.StartsWith("meta:", StringComparison.Ordinal)) continue;
            count++;
        }
        return count;
    }
}

