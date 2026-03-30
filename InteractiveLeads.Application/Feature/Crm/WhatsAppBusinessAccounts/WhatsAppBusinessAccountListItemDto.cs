namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

public sealed class WhatsAppBusinessAccountListItemDto
{
    public Guid Id { get; set; }

    public string WabaId { get; set; } = string.Empty;

    public string? Name { get; set; }

    public int IntegrationCount { get; set; }
}
