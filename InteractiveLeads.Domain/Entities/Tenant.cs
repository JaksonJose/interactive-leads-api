namespace InteractiveLeads.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Identifier { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Company> Companies { get; set; } = new List<Company>();
}

