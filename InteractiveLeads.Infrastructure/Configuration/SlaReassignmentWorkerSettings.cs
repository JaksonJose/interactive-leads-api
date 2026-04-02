namespace InteractiveLeads.Infrastructure.Configuration;

/// <summary>Background scan for first-response SLA breaches and auto-reassign to another agent.</summary>
public sealed class SlaReassignmentWorkerSettings
{
    public const string SectionName = "SlaReassignmentWorker";

    /// <summary>Delay between full cycles (all tenants). Minimum 15 seconds.</summary>
    public int IntervalSeconds { get; set; } = 45;

    /// <summary>Max conversations processed per tenant per cycle.</summary>
    public int BatchSize { get; set; } = 50;
}
