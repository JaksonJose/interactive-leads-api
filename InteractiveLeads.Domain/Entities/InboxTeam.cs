namespace InteractiveLeads.Domain.Entities;

public class InboxTeam
{
    public Guid Id { get; set; }
    public Guid InboxId { get; set; }
    public Guid TeamId { get; set; }

    public Inbox Inbox { get; set; } = default!;
    public Team Team { get; set; } = default!;
}
