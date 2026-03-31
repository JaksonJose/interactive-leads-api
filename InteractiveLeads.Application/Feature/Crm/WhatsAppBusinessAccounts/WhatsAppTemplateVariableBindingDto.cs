namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

/// <summary>Maps a Meta template placeholder index to a CRM merge field (per template).</summary>
public sealed class WhatsAppTemplateVariableBindingDto
{
    /// <summary>1-based slot as in <c>{{1}}</c> within the section.</summary>
    public int Slot { get; set; }

    /// <summary>CRM semantic key, e.g. <c>contact.name</c>.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Sample value used for Meta template examples (submission).</summary>
    public string? Example { get; set; }

    /// <summary><c>header</c> or <c>body</c>.</summary>
    public string Section { get; set; } = "body";
}
