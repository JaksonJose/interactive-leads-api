namespace InteractiveLeads.Infrastructure.Configuration;

/// <summary>Client timeouts for StackExchange.Redis (remote servers often need &gt; 5s).</summary>
public sealed class RedisConnectionOptions
{
    public const string SectionName = "Redis";

    /// <summary>Default command timeout (ms). StackExchange.Redis default is 5000.</summary>
    public int SyncTimeoutMs { get; set; } = 15000;

    /// <summary>Async command timeout (ms). Defaults to <see cref="SyncTimeoutMs"/> if unset in config.</summary>
    public int? AsyncTimeoutMs { get; set; }

    /// <summary>TCP connect timeout (ms).</summary>
    public int ConnectTimeoutMs { get; set; } = 15000;
}
