namespace InteractiveLeads.Domain.Entities;

public class Company
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Document { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = default!;
    public ICollection<Integration> Integrations { get; set; } = new List<Integration>();
    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}

