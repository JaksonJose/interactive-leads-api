using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.TemplateQueue;

/// <summary>Builds Meta Cloud API-style template <c>components</c> from CRM form fields.</summary>
public static class WhatsAppTemplateMetaComponentsBuilder
{
    public static List<Dictionary<string, object?>> Build(CreateWhatsAppTemplateRequest t)
    {
        var list = new List<Dictionary<string, object?>>();
        var header = string.IsNullOrWhiteSpace(t.HeaderText) ? null : t.HeaderText.Trim();
        if (header != null)
        {
            var h = new Dictionary<string, object?>
            {
                ["type"] = "HEADER",
                ["format"] = "TEXT",
                ["text"] = header
            };
            if (!string.IsNullOrWhiteSpace(t.HeaderExample))
            {
                h["example"] = new Dictionary<string, object?>
                {
                    ["header_text"] = new[] { t.HeaderExample.Trim() }
                };
            }

            list.Add(h);
        }

        var body = new Dictionary<string, object?>
        {
            ["type"] = "BODY",
            ["text"] = (t.Body ?? string.Empty).Trim()
        };
        if (t.BodyExamples is { Length: > 0 })
        {
            body["example"] = new Dictionary<string, object?>
            {
                ["body_text"] = new[] { t.BodyExamples.Select(x => x ?? string.Empty).ToArray() }
            };
        }

        list.Add(body);

        if (!string.IsNullOrWhiteSpace(t.Footer))
        {
            list.Add(new Dictionary<string, object?>
            {
                ["type"] = "FOOTER",
                ["text"] = t.Footer.Trim()
            });
        }

        if (t.Buttons is { Length: > 0 })
        {
            var buttons = new List<Dictionary<string, object?>>();
            foreach (var b in t.Buttons)
            {
                var type = (b.Type ?? string.Empty).Trim().ToUpperInvariant();
                var row = new Dictionary<string, object?>
                {
                    ["type"] = type,
                    ["text"] = (b.Text ?? string.Empty).Trim()
                };
                if (type == "URL" && !string.IsNullOrWhiteSpace(b.Url))
                    row["url"] = b.Url.Trim();
                if (type == "PHONE_NUMBER" && !string.IsNullOrWhiteSpace(b.PhoneNumber))
                    row["phone_number"] = b.PhoneNumber.Trim();
                buttons.Add(row);
            }

            list.Add(new Dictionary<string, object?>
            {
                ["type"] = "BUTTONS",
                ["buttons"] = buttons
            });
        }

        return list;
    }
}
