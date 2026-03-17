namespace InteractiveLeads.Domain.Entities;

public class ContactChannel
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public Guid IntegrationId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Contact Contact { get; set; } = default!;
    public Integration Integration { get; set; } = default!;
}

