namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

public sealed class WhatsAppBusinessAccountDto
{
    public Guid Id { get; set; }

    /// <summary>Meta WABA id.</summary>
    public string WabaId { get; set; } = string.Empty;

    public string? Name { get; set; }
}
