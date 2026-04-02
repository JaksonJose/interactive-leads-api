namespace InteractiveLeads.Infrastructure.Configuration;

/// <summary>Background scans for SLA / inactivity auto-reassign.</summary>
public sealed class SlaReassignmentWorkerSettings
{
    public const string SectionName = "SlaReassignmentWorker";

    /// <summary>Delay between cycles for inactivity reassignment worker. Minimum 2 seconds.</summary>
    public int IntervalSeconds { get; set; } = 5;

    /// <summary>Delay between cycles for first-response SLA expiry reassignment. Minimum 5 seconds.</summary>
    public int FirstResponseScanIntervalSeconds { get; set; } = 5;

    /// <summary>Max conversations processed per tenant per cycle.</summary>
    public int BatchSize { get; set; } = 50;
}
