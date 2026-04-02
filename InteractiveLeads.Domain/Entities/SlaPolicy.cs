namespace InteractiveLeads.Domain.Entities;

/// <summary>SLA targets for first response and resolution, scoped to a company.</summary>
public class SlaPolicy
{
    public const int MaxNameLength = 256;
    public const int MaxDescriptionLength = 2000;
    public const int MaxCodeLength = 64;

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }

    /// <summary>Optional stable code for reporting/BI (unique per company when set).</summary>
    public string? Code { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int FirstResponseTargetMinutes { get; set; }
    public int ResolutionTargetMinutes { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = default!;
    public Company Company { get; set; } = default!;
}
