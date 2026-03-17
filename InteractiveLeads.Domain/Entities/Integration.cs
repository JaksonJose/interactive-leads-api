using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Domain.Entities;

public class Integration
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public IntegrationType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ExternalIdentifier { get; set; } = string.Empty;
    public string? Settings { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Company Company { get; set; } = default!;
    public ICollection<ContactChannel> ContactChannels { get; set; } = new List<ContactChannel>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}

