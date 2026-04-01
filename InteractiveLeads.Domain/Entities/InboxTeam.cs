namespace InteractiveLeads.Domain.Entities;

public class InboxTeam
{
    public Guid Id { get; set; }
    public Guid InboxId { get; set; }
    public Guid TeamId { get; set; }

    /// <summary>Lower value = tried first when routing a new conversation (1, 2, 3…).</summary>
    public int Priority { get; set; }

    public Inbox Inbox { get; set; } = default!;
    public Team Team { get; set; } = default!;
}
