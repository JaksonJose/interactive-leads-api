namespace InteractiveLeads.Application.Feature.Chat.Inboxes;

public sealed class InboxDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Guid? DefaultCalendarId { get; set; }

    public Guid? DefaultSlaPolicyId { get; set; }
}

