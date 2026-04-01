using System.Globalization;
using System.Linq;
using InteractiveLeads.Application.Realtime.Services.Presence;
using InteractiveLeads.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace InteractiveLeads.Infrastructure.Realtime.Presence;

public sealed class RedisPresenceService(
    IConnectionMultiplexer mux,
    IOptions<PresenceOptions> presenceOptions) : IPresenceService
{
    private readonly IDatabase _db = mux.GetDatabase();
    private readonly int _sessionTtlSeconds = Math.Max(30, presenceOptions.Value.SessionTtlSeconds);

    private static RedisKey UserKey(string tenantId, string userId) => $"presence:user:{tenantId}:{userId}";
    private static RedisKey ConnKey(string connectionId) => $"presence:conn:{connectionId}";
    private static RedisValue UserConnField(string connectionId) => connectionId;

    private IServer Server => mux.GetServer(mux.GetEndPoints().First());

    public async Task<PresenceStateDto> ConnectionOpenedAsync(
        string tenantId,
        string userId,
        string connectionId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var connMap = new HashEntry[]
        {
            new("tenantId", tenantId),
            new("userId", userId),
        };

        var userKey = UserKey(tenantId, userId);
        var tran = _db.CreateTransaction();
        _ = tran.HashSetAsync(ConnKey(connectionId), connMap);
        _ = tran.HashSetAsync(userKey, new[] { new HashEntry(UserConnField(connectionId), "1") });
        _ = tran.HashDeleteAsync(userKey, "lastSeenAtUtc");

        var exec = await tran.ExecuteAsync();
        if (!exec)
        {
            return new PresenceStateDto(tenantId, userId, true, null);
        }

        await _db.KeyExpireAsync(ConnKey(connectionId), TimeSpan.FromSeconds(_sessionTtlSeconds));

        var live = await PruneStaleAndCountLiveAsync(userKey, ct);
        return new PresenceStateDto(tenantId, userId, live > 0, null);
    }

    public async Task<PresenceStateDto?> ConnectionClosedAsync(string connectionId, CancellationToken ct)
    {
        var connKey = ConnKey(connectionId);
        var map = await _db.HashGetAllAsync(connKey);
        if (map is null || map.Length == 0)
        {
            // Session TTL may have expired before SignalR ran disconnect; the user hash can still list this
            // connectionId. Remove it and recompute so logout / kill doesn't leave a permanent "online" ghost.
            return await FinalizeDisconnectAfterConnKeyMissingAsync(connectionId, ct);
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

        var live = await PruneStaleAndCountLiveAsync(userKey, ct);
        var online = live > 0;
        DateTimeOffset? lastSeen = null;
        if (!online)
        {
            lastSeen = DateTimeOffset.UtcNow;
            await _db.HashSetAsync(userKey, new[] { new HashEntry("lastSeenAtUtc", lastSeen.Value.ToString("O", CultureInfo.InvariantCulture)) });
        }

        return new PresenceStateDto(tenantId!, userId!, online, lastSeen);
    }

    /// <summary>
    /// When <c>presence:conn:{id}</c> is already gone (expired), locate the user hash that still references
    /// <paramref name="connectionId"/> as a field name and remove it, then prune/count like a normal disconnect.
    /// </summary>
    private async Task<PresenceStateDto?> FinalizeDisconnectAfterConnKeyMissingAsync(string connectionId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var field = UserConnField(connectionId);
        RedisKey? userKeyFound = null;

        foreach (var key in Server.Keys(pattern: "presence:user:*"))
        {
            ct.ThrowIfCancellationRequested();
            if (await _db.HashDeleteAsync(key, field))
            {
                userKeyFound = key;
                break;
            }
        }

        if (userKeyFound is not RedisKey userKey)
        {
            return null;
        }

        if (!TryParseUserKey(userKey, out var tenantId, out var userId))
        {
            return null;
        }

        var live = await PruneStaleAndCountLiveAsync(userKey, ct);
        var online = live > 0;
        DateTimeOffset? lastSeen = null;
        if (!online)
        {
            lastSeen = DateTimeOffset.UtcNow;
            await _db.HashSetAsync(userKey, new[] { new HashEntry("lastSeenAtUtc", lastSeen.Value.ToString("O", CultureInfo.InvariantCulture)) });
        }

        return new PresenceStateDto(tenantId, userId, online, lastSeen);
    }

    private static bool TryParseUserKey(RedisKey userKey, out string tenantId, out string userId)
    {
        tenantId = string.Empty;
        userId = string.Empty;
        var s = userKey.ToString();
        var parts = s.Split(':');
        if (parts.Length < 4) return false;
        tenantId = parts[2];
        userId = string.Join(":", parts.Skip(3));
        return !string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(userId);
    }

    public async Task<PresenceStateDto?> HeartbeatAsync(
        string connectionId,
        string tenantId,
        string userId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var connKey = ConnKey(connectionId);
        var map = await _db.HashGetAllAsync(connKey);
        if (map is null || map.Length == 0)
        {
            return null;
        }

        string? mapTenant = null;
        string? mapUser = null;
        foreach (var e in map)
        {
            if (e.Name == "tenantId") mapTenant = e.Value.ToString();
            else if (e.Name == "userId") mapUser = e.Value.ToString();
        }

        if (string.IsNullOrWhiteSpace(mapTenant) || string.IsNullOrWhiteSpace(mapUser)
            || !SameTenant(mapTenant, tenantId) || !SameUser(mapUser, userId))
        {
            return null;
        }

        await _db.KeyExpireAsync(connKey, TimeSpan.FromSeconds(_sessionTtlSeconds));

        var userKey = UserKey(tenantId, userId);
        await PruneStaleAndCountLiveAsync(userKey, ct);
        // Caller remains online while this connection heartbeats; offline transitions use disconnect or TTL+list prune.
        return null;
    }

    public async Task<IReadOnlyList<PresenceStateDto>> ListTenantPresenceAsync(string tenantId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var keys = Server.Keys(pattern: $"presence:user:{tenantId}:*").ToArray();

        var list = new List<PresenceStateDto>(keys.Length);
        foreach (var k in keys)
        {
            ct.ThrowIfCancellationRequested();
            var parts = k.ToString().Split(':');
            if (parts.Length < 4) continue;
            var userId = parts[^1];

            var entriesBefore = await _db.HashGetAllAsync(k);
            var hadConnFields = entriesBefore.Any(e => IsConnectionFieldName(e.Name.ToString()));
            var live = await PruneStaleAndCountLiveAsync(k, ct);

            DateTimeOffset? lastSeen = null;
            if (live == 0 && hadConnFields)
            {
                var now = DateTimeOffset.UtcNow;
                await _db.HashSetAsync(k, new[] { new HashEntry("lastSeenAtUtc", now.ToString("O", CultureInfo.InvariantCulture)) });
                lastSeen = now;
            }
            else
            {
                var entries = await _db.HashGetAllAsync(k);
                var lastSeenRaw = entries.FirstOrDefault(e => e.Name == "lastSeenAtUtc").Value;
                if (lastSeenRaw.HasValue && DateTimeOffset.TryParse(lastSeenRaw.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                    lastSeen = dt;
            }
            list.Add(new PresenceStateDto(tenantId, userId, live > 0, lastSeen));
        }
        return list;
    }

    private async Task<int> PruneStaleAndCountLiveAsync(RedisKey userKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var entries = await _db.HashGetAllAsync(userKey);
        var live = 0;
        foreach (var e in entries)
        {
            var n = e.Name.ToString();
            if (!IsConnectionFieldName(n)) continue;
            var ck = ConnKey(n);
            if (await _db.KeyExistsAsync(ck))
                live++;
            else
                await _db.HashDeleteAsync(userKey, n);
        }
        return live;
    }

    private static bool IsConnectionFieldName(string name)
    {
        if (string.IsNullOrEmpty(name) || name == "lastSeenAtUtc") return false;
        if (name.StartsWith("meta:", StringComparison.Ordinal)) return false;
        return true;
    }

    private static bool SameUser(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
        if (Guid.TryParse(a.Trim(), out var ga) && Guid.TryParse(b.Trim(), out var gb))
            return ga == gb;
        return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool SameTenant(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
        if (Guid.TryParse(a.Trim(), out var ga) && Guid.TryParse(b.Trim(), out var gb))
            return ga == gb;
        return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
