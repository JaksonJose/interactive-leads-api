namespace InteractiveLeads.Domain.Entities;

public class Inbox
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Fallback business hours when the routed team has no calendar.</summary>
    public Guid? DefaultCalendarId { get; set; }

    /// <summary>Fallback SLA policy when the routed team has none.</summary>
    public Guid? DefaultSlaPolicyId { get; set; }

    public SlaPolicy? DefaultSlaPolicy { get; set; }

    public Company Company { get; set; } = default!;
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<InboxTeam> TeamLinks { get; set; } = new List<InboxTeam>();
}
