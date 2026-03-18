using System.Text.Json.Serialization;

namespace InteractiveLeads.Application.Realtime.Models;

public class RealtimeEvent<T>
{
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Tenant identifier to help clients/consumers validate ordering/isolation.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public T Payload { get; set; } = default!;
}

