namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

/// <summary>Allowed semantic merge keys for WhatsApp template authoring (MVP).</summary>
public static class WhatsAppTemplateVariableCatalog
{
    /// <summary>Semantic tokens use lowercase with dots (e.g. <c>contact.name</c>).</summary>
    public static readonly HashSet<string> AllowedSources = new(StringComparer.OrdinalIgnoreCase)
    {
        "contact.name",
        "contact.phone",
        "contact.email",
        "user.display_name",
        "user.email",
        "user.phone",
        "company.name",
        "company.document",
        "agent.display_name",
        "agent.email",
        "agent.phone"
    };

    public const int MaxBodyNumericSlots = 10;

    public const int MaxHeaderNumericSlots = 1;

    public static string DefaultExampleForSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return "—";
        return source.Trim().ToLowerInvariant() switch
        {
            "contact.name" => "Maria",
            "contact.phone" => "5511999999999",
            "contact.email" => "maria@example.com",
            "user.display_name" => "Equipe",
            "user.email" => "equipe@example.com",
            "user.phone" => "5511888888888",
            "company.name" => "Minha Empresa",
            "company.document" => "00000000000100",
            "agent.display_name" => "João",
            "agent.email" => "joao@example.com",
            "agent.phone" => "5511777777777",
            _ => "—"
        };
    }

    public static bool IsAllowedSource(string token) =>
        !string.IsNullOrWhiteSpace(token) && AllowedSources.Contains(token.Trim());
}
