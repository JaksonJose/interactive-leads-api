namespace InteractiveLeads.Domain.Entities;

/// <summary>WhatsApp Business Account (WABA) at Meta. Templates and shared config are scoped here; each <see cref="Integration"/> phone number links to one WABA.</summary>
public class WhatsAppBusinessAccount
{
    public Guid Id { get; set; }

    /// <summary>Meta WABA id (same value as <c>businessAccountId</c> in <see cref="Settings.WhatsAppSettings"/>).</summary>
    public string WabaId { get; set; } = string.Empty;

    public Guid CompanyId { get; set; }

    public string? Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Company Company { get; set; } = default!;

    public ICollection<Integration> Integrations { get; set; } = new List<Integration>();

    public ICollection<WhatsAppTemplate> Templates { get; set; } = new List<WhatsAppTemplate>();
}
