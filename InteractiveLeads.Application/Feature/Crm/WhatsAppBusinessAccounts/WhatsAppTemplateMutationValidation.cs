using InteractiveLeads.Application.Responses;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

internal static class WhatsAppTemplateMutationValidation
{
    internal static readonly HashSet<string> AllowedCategories =
    [
        "UTILITY",
        "MARKETING",
        "AUTHENTICATION"
    ];

    internal static readonly HashSet<string> AllowedButtonTypes =
    [
        "QUICK_REPLY",
        "URL",
        "PHONE_NUMBER"
    ];

    /// <summary>Validates category, body, header, footer, buttons (not name/language).</summary>
    public static ResultResponse? ValidateEditableFields(
        string categoryRaw,
        string bodyRaw,
        string? headerText,
        string? footer,
        CreateWhatsAppTemplateButtonRequest[]? buttons)
    {
        var category = categoryRaw.Trim().ToUpperInvariant();
        var body = bodyRaw.Trim();
        var badRequest = new ResultResponse();

        if (!AllowedCategories.Contains(category))
            badRequest.AddErrorMessage("Template category is invalid.", "integrations.templates.invalid_category");
        if (body.Length is < 1 or > 1024)
            badRequest.AddErrorMessage("Template body length is invalid.", "integrations.templates.invalid_body");

        if (!string.IsNullOrWhiteSpace(headerText))
        {
            var h = headerText.Trim();
            if (h.Length > 60)
                badRequest.AddErrorMessage("Header text is too long.", "integrations.templates.invalid_header");
        }

        if (!string.IsNullOrWhiteSpace(footer))
        {
            var f = footer.Trim();
            if (f.Length > 60)
                badRequest.AddErrorMessage("Footer is too long.", "integrations.templates.invalid_footer");
        }

        if (buttons is { Length: > 0 })
        {
            foreach (var b in buttons)
            {
                var type = (b.Type ?? string.Empty).Trim().ToUpperInvariant();
                if (!AllowedButtonTypes.Contains(type))
                {
                    badRequest.AddErrorMessage("Invalid button type.", "integrations.templates.invalid_button");
                    break;
                }

                var text = (b.Text ?? string.Empty).Trim();
                if (text.Length is < 1 or > 64)
                {
                    badRequest.AddErrorMessage("Button text length is invalid.", "integrations.templates.invalid_button");
                    break;
                }

                if (type == "URL" && string.IsNullOrWhiteSpace(b.Url))
                    badRequest.AddErrorMessage("URL button requires url.", "integrations.templates.invalid_button");
                if (type == "PHONE_NUMBER" && string.IsNullOrWhiteSpace(b.PhoneNumber))
                    badRequest.AddErrorMessage("Phone button requires phoneNumber.", "integrations.templates.invalid_button");
            }
        }

        return badRequest.Messages is { Count: > 0 } ? badRequest : null;
    }
}
