namespace InteractiveLeads.Domain.Entities;

/// <summary>
/// Logical grouping of users within a company. Optional links to inboxes support routing and future SLA/auto-assignment.
/// </summary>
public class Team
{
    public const int MaxNameLength = 256;
    public const int MaxDescriptionLength = 2000;

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Optional calendar for future business-hours integration.</summary>
    public Guid? CalendarId { get; set; }

    /// <summary>Optional FK placeholder for future team-based SLA policy.</summary>
    public Guid? SlaPolicyId { get; set; }

    public Tenant Tenant { get; set; } = default!;
    public Company Company { get; set; } = default!;
    public ICollection<UserTeam> Members { get; set; } = new List<UserTeam>();
    public ICollection<InboxTeam> InboxLinks { get; set; } = new List<InboxTeam>();

    public void Rename(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("Team name is required.", nameof(name));
        if (trimmed.Length > MaxNameLength)
            throw new ArgumentException($"Team name cannot exceed {MaxNameLength} characters.", nameof(name));
        Name = trimmed;
    }

    public void SetDescription(string? description)
    {
        if (description is null)
        {
            Description = null;
            return;
        }
        var trimmed = description.Trim();
        if (trimmed.Length > MaxDescriptionLength)
            throw new ArgumentException($"Description cannot exceed {MaxDescriptionLength} characters.", nameof(description));
        Description = string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    public void Deactivate() => IsActive = false;
}
