namespace InteractiveLeads.Domain.Entities;

public class Inbox
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Company Company { get; set; } = default!;
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<InboxTeam> TeamLinks { get; set; } = new List<InboxTeam>();
}
