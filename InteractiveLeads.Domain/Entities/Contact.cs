namespace InteractiveLeads.Domain.Entities;

public class Contact
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public Company Company { get; set; } = default!;
    public ICollection<ContactChannel> ContactChannels { get; set; } = new List<ContactChannel>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}

