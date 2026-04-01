namespace InteractiveLeads.Infrastructure.Configuration;

/// <summary>Redis-backed SignalR presence: per-connection session TTL and application heartbeat.</summary>
public sealed class PresenceOptions
{
    public const string SectionName = "Presence";

    /// <summary>TTL for <c>presence:conn:{connectionId}</c>; renewed by each heartbeat. Default 120s.</summary>
    public int SessionTtlSeconds { get; set; } = 120;
}
