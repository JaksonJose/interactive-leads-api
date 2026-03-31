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
        {
            WhatsAppTemplateVariableBindingStatus.Recalculate(dto);
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(componentsJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("source", out var srcEl)
                && string.Equals(srcEl.GetString(), "meta_template_sync", StringComparison.OrdinalIgnoreCase))
            {
                ApplyMetaTemplateSync(dto, root);
                TryApplyVariableBindingsFromRoot(dto, root);
                WhatsAppTemplateVariableBindingStatus.Recalculate(dto);
                return;
            }

            if (LooksLikePersistedSchema(root))
            {
                if (TryApplyPersistedComponents(dto, componentsJson))
                {
                    WhatsAppTemplateVariableBindingStatus.Recalculate(dto);
                    return;
                }
            }

            if (LooksLikeLegacyCrmRequest(root))
            {
                TryApplyLegacyCreateRequest(dto, componentsJson);
                TryApplyVariableBindingsFromRoot(dto, root);
                WhatsAppTemplateVariableBindingStatus.Recalculate(dto);
                return;
            }

            TryApplyLegacyCreateRequest(dto, componentsJson);
            TryApplyVariableBindingsFromRoot(dto, root);
        }
        catch (JsonException)
        {
            TryApplyLegacyCreateRequest(dto, componentsJson);
        }

        WhatsAppTemplateVariableBindingStatus.Recalculate(dto);
    }

    private static bool LooksLikePersistedSchema(JsonElement root) =>
        (root.TryGetProperty("schemaVersion", out var sv) && sv.ValueKind == JsonValueKind.Number)
        || (root.TryGetProperty("variableBindings", out _) && root.TryGetProperty("body", out var b) && b.ValueKind == JsonValueKind.String);

    private static bool LooksLikeLegacyCrmRequest(JsonElement root) =>
        root.TryGetProperty("body", out var b) && b.ValueKind == JsonValueKind.String;

    private static bool TryApplyPersistedComponents(WhatsAppTemplateDetailDto dto, string componentsJson)
    {
        try
        {
            var persisted = JsonSerializer.Deserialize<WhatsAppTemplatePersistedComponents>(componentsJson, JsonOpts);
            if (persisted == null)
                return false;

            dto.AuthoringHeaderText = persisted.AuthoringHeaderText;
            dto.AuthoringBody = persisted.AuthoringBody;
            dto.HeaderText = persisted.HeaderText;
            dto.HeaderExample = persisted.HeaderExample;
            dto.Body = persisted.Body ?? string.Empty;
            dto.BodyExamples = persisted.BodyExamples;
            dto.Footer = persisted.Footer;
            dto.Buttons = persisted.Buttons;
            dto.VariableBindings = persisted.VariableBindings;
            dto.IsMetaSynced = persisted.IsMetaSynced;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void TryApplyLegacyCreateRequest(WhatsAppTemplateDetailDto dto, string componentsJson)
    {
        try
        {
            var req = JsonSerializer.Deserialize<CreateWhatsAppTemplateRequest>(componentsJson, JsonOpts);
            if (req == null)
                return;

            dto.AuthoringHeaderText = req.AuthoringHeaderText;
            dto.AuthoringBody = req.AuthoringBody;
            dto.HeaderText = req.HeaderText;
            dto.HeaderExample = req.HeaderExample;
            dto.Body = req.Body ?? string.Empty;
            dto.BodyExamples = req.BodyExamples;
            dto.Footer = req.Footer;
            dto.Buttons = req.Buttons;
            dto.IsMetaSynced = false;
        }
        catch (JsonException)
        {
            // leave display fields as-is
        }
    }

    private static void TryApplyVariableBindingsFromRoot(WhatsAppTemplateDetailDto dto, JsonElement root)
    {
        if (!root.TryGetProperty("variableBindings", out var vb) || vb.ValueKind != JsonValueKind.Array)
            return;

        dto.VariableBindings = ParseVariableBindingsArray(vb);
    }

    private static WhatsAppTemplateVariableBindingDto[]? ParseVariableBindingsArray(JsonElement vb)
    {
        var list = new List<WhatsAppTemplateVariableBindingDto>();
        foreach (var el in vb.EnumerateArray())
        {
            if (el.ValueKind != JsonValueKind.Object)
                continue;
            var slot = 0;
            if (el.TryGetProperty("slot", out var s) && s.ValueKind == JsonValueKind.Number)
                slot = s.GetInt32();
            var source = GetString(el, "source") ?? string.Empty;
            var example = GetString(el, "example");
            var section = GetString(el, "section") ?? "body";
            if (slot < 1 || string.IsNullOrWhiteSpace(source))
                continue;
            list.Add(new WhatsAppTemplateVariableBindingDto
            {
                Slot = slot,
                Source = source.Trim(),
                Example = example,
                Section = section.Trim().ToLowerInvariant()
            });
        }

        return list.Count > 0 ? list.ToArray() : null;
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

        dto.IsMetaSynced = true;
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
