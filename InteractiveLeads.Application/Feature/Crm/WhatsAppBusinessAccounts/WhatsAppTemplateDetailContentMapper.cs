using System.Text.Json;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

/// <summary>Maps <see cref="InteractiveLeads.Domain.Entities.WhatsAppTemplate"/>.<c>ComponentsJson</c> into display fields on <see cref="WhatsAppTemplateDetailDto"/>.</summary>
internal static class WhatsAppTemplateDetailContentMapper
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static void HydrateFromComponentsJson(WhatsAppTemplateDetailDto dto, string componentsJson)
    {
        if (string.IsNullOrWhiteSpace(componentsJson) || componentsJson.Trim() == "{}")
            return;

        try
        {
            using var doc = JsonDocument.Parse(componentsJson);
            var root = doc.RootElement;

            if (LooksLikeCrmSerializedForm(root))
            {
                TryApplyCrmForm(dto, componentsJson);
                return;
            }

            if (root.TryGetProperty("source", out var srcEl)
                && string.Equals(srcEl.GetString(), "meta_template_sync", StringComparison.OrdinalIgnoreCase))
            {
                ApplyMetaTemplateSync(dto, root);
                return;
            }

            TryApplyCrmForm(dto, componentsJson);
        }
        catch (JsonException)
        {
            TryApplyCrmForm(dto, componentsJson);
        }
    }

    private static bool LooksLikeCrmSerializedForm(JsonElement root) =>
        root.TryGetProperty("body", out var b) && b.ValueKind == JsonValueKind.String;

    private static void TryApplyCrmForm(WhatsAppTemplateDetailDto dto, string componentsJson)
    {
        try
        {
            var req = JsonSerializer.Deserialize<CreateWhatsAppTemplateRequest>(componentsJson, JsonOpts);
            if (req == null)
                return;

            dto.HeaderText = req.HeaderText;
            dto.HeaderExample = req.HeaderExample;
            dto.Body = req.Body ?? string.Empty;
            dto.BodyExamples = req.BodyExamples;
            dto.Footer = req.Footer;
            dto.Buttons = req.Buttons;
        }
        catch (JsonException)
        {
            // leave display fields empty
        }
    }

    private static void ApplyMetaTemplateSync(WhatsAppTemplateDetailDto dto, JsonElement root)
    {
        if (!root.TryGetProperty("components", out var comps) || comps.ValueKind != JsonValueKind.Array)
            return;

        var bodyExampleLines = new List<string>();
        foreach (var comp in comps.EnumerateArray())
        {
            var type = GetString(comp, "type")?.ToUpperInvariant();
            switch (type)
            {
                case "HEADER":
                    if (string.Equals(GetString(comp, "format"), "TEXT", StringComparison.OrdinalIgnoreCase))
                        dto.HeaderText = GetString(comp, "text");
                    break;
                case "BODY":
                    dto.Body = GetString(comp, "text") ?? string.Empty;
                    if (comp.TryGetProperty("example", out var ex) && ex.TryGetProperty("body_text", out var bt))
                        CollectBodyExamples(bt, bodyExampleLines);
                    break;
                case "FOOTER":
                    dto.Footer = GetString(comp, "text");
                    break;
                case "BUTTONS":
                    dto.Buttons = MapMetaButtons(comp);
                    break;
            }
        }

        if (bodyExampleLines.Count > 0)
            dto.BodyExamples = bodyExampleLines.ToArray();
    }

    private static void CollectBodyExamples(JsonElement bodyText, List<string> lines)
    {
        if (bodyText.ValueKind != JsonValueKind.Array)
            return;

        foreach (var row in bodyText.EnumerateArray())
        {
            if (row.ValueKind == JsonValueKind.Array)
            {
                foreach (var cell in row.EnumerateArray())
                    AppendExampleCell(cell, lines);
            }
            else
                AppendExampleCell(row, lines);
        }
    }

    private static void AppendExampleCell(JsonElement el, List<string> lines)
    {
        if (el.ValueKind == JsonValueKind.String)
        {
            var s = el.GetString();
            if (!string.IsNullOrEmpty(s))
                lines.Add(s);
        }
    }

    private static CreateWhatsAppTemplateButtonRequest[]? MapMetaButtons(JsonElement buttonsComponent)
    {
        if (!buttonsComponent.TryGetProperty("buttons", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return null;

        var list = new List<CreateWhatsAppTemplateButtonRequest>();
        foreach (var b in arr.EnumerateArray())
        {
            var metaType = GetString(b, "type")?.ToUpperInvariant() ?? string.Empty;
            var text = GetString(b, "text") ?? string.Empty;
            var btn = new CreateWhatsAppTemplateButtonRequest
            {
                Type = metaType switch
                {
                    "URL" => "URL",
                    "PHONE_NUMBER" => "PHONE_NUMBER",
                    "QUICK_REPLY" => "QUICK_REPLY",
                    _ => metaType
                },
                Text = text
            };

            if (metaType == "URL")
                btn.Url = GetString(b, "url");
            if (metaType == "PHONE_NUMBER")
                btn.PhoneNumber = GetString(b, "phone_number") ?? GetString(b, "phoneNumber");

            list.Add(btn);
        }

        return list.Count > 0 ? list.ToArray() : null;
    }

    private static string? GetString(JsonElement el, string property)
    {
        if (!el.TryGetProperty(property, out var p))
            return null;

        return p.ValueKind switch
        {
            JsonValueKind.String => p.GetString(),
            JsonValueKind.Number => p.GetRawText(),
            _ => null
        };
    }
}
