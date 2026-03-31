using InteractiveLeads.Domain.Entities;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

/// <summary>Resolves <see cref="WhatsAppTemplateVariableBindingDto"/> to WhatsApp template parameter strings for send.</summary>
public static class WhatsAppTemplateParameterResolver
{
    private const string EmptyFallback = "-";

    public static void Resolve(
        WhatsAppTemplateDetailDto detail,
        Contact contact,
        Company company,
        (string? DisplayName, string? Email) sender,
        (string? DisplayName, string? Email)? assignedAgent,
        out string? headerParameter,
        out string[] bodyParameters)
    {
        headerParameter = null;
        bodyParameters = [];
        var bindings = detail.VariableBindings ?? [];
        var headerBindings = bindings
            .Where(b => string.Equals(b.Section, "header", StringComparison.OrdinalIgnoreCase))
            .OrderBy(b => b.Slot)
            .ToList();
        var bodyBindings = bindings
            .Where(b => string.Equals(b.Section, "body", StringComparison.OrdinalIgnoreCase))
            .OrderBy(b => b.Slot)
            .ToList();

        if (headerBindings.Count > 0)
            headerParameter = FormatValue(GetRawValue(headerBindings[0].Source, contact, company, sender, assignedAgent));

        if (bodyBindings.Count > 0)
            bodyParameters = bodyBindings
                .Select(b => FormatValue(GetRawValue(b.Source, contact, company, sender, assignedAgent)))
                .ToArray();
    }

    private static string FormatValue(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? EmptyFallback : raw.Trim();

    private static string? GetRawValue(
        string source,
        Contact contact,
        Company company,
        (string? DisplayName, string? Email) sender,
        (string? DisplayName, string? Email)? assignedAgent)
    {
        var key = (source ?? string.Empty).Trim().ToLowerInvariant();
        var agent = assignedAgent ?? sender;

        return key switch
        {
            "contact.name" => contact.Name,
            "contact.phone" => contact.Phone,
            "contact.email" => contact.Email,
            "company.name" => company.Name,
            "company.document" => company.Document,
            "user.display_name" => sender.DisplayName,
            "user.email" => sender.Email,
            "user.phone" => null,
            "agent.display_name" => agent.DisplayName,
            "agent.email" => agent.Email,
            "agent.phone" => null,
            _ => null
        };
    }
}
