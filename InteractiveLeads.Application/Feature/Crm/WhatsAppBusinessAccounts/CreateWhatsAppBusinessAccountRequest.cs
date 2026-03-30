namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

public sealed class CreateWhatsAppBusinessAccountRequest
{
    /// <summary>Meta WhatsApp Business Account id.</summary>
    public string WabaId { get; set; } = string.Empty;

    public string? Name { get; set; }
}
