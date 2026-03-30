using InteractiveLeads.Application.Responses;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

internal static class WhatsAppTemplateVariableBindingsValidator
{
    /// <summary>
    /// Ensures each Meta placeholder slot in header/body has exactly one binding, no extras, and sources are allowed.
    /// </summary>
    public static ResultResponse? Validate(WhatsAppTemplateDetailDto template, IReadOnlyList<WhatsAppTemplateVariableBindingDto> bindings)
    {
        var headerSlots = WhatsAppTemplatePlaceholderCompiler.ParseMetaSlots(template.HeaderText).OrderBy(x => x).ToList();
        var bodySlots = WhatsAppTemplatePlaceholderCompiler.ParseMetaSlots(template.Body).OrderBy(x => x).ToList();
        var expectedCount = headerSlots.Count + bodySlots.Count;

        if (expectedCount == 0)
        {
            if (bindings.Count > 0)
            {
                var err = new ResultResponse();
                err.AddErrorMessage(
                    "This template has no message variables; remove variable bindings.",
                    "integrations.templates.bindings_none_expected");
                return err;
            }

            return null;
        }

        if (bindings.Count != expectedCount)
        {
            var err = new ResultResponse();
            err.AddErrorMessage(
                $"This template requires exactly {expectedCount} variable binding(s) for Meta placeholders.",
                "integrations.templates.bindings_slot_mismatch");
            return err;
        }

        var seen = new HashSet<(string Section, int Slot)>();
        foreach (var b in bindings)
        {
            var section = (b.Section ?? "body").Trim().ToLowerInvariant();
            if (section is not ("header" or "body"))
            {
                var err = new ResultResponse();
                err.AddErrorMessage(
                    "Each binding must use section \"header\" or \"body\".",
                    "integrations.templates.bindings_invalid_section");
                return err;
            }

            if (b.Slot < 1)
            {
                var err = new ResultResponse();
                err.AddErrorMessage("Each binding must have a positive slot number.", "integrations.templates.bindings_invalid_slot");
                return err;
            }

            if (!WhatsAppTemplateVariableCatalog.IsAllowedSource(b.Source))
            {
                var err = new ResultResponse();
                err.AddErrorMessage($"Unknown or disallowed variable source: {b.Source}.", "integrations.templates.unknown_variable");
                return err;
            }

            if (!seen.Add((section, b.Slot)))
            {
                var err = new ResultResponse();
                err.AddErrorMessage(
                    "Duplicate binding for the same section and slot.",
                    "integrations.templates.bindings_duplicate_slot");
                return err;
            }

            if (section == "header")
            {
                if (!headerSlots.Contains(b.Slot))
                {
                    var err = new ResultResponse();
                    err.AddErrorMessage(
                        $"Header has no {{{{{b.Slot}}}}} placeholder for this binding.",
                        "integrations.templates.bindings_unexpected");
                    return err;
                }
            }
            else if (!bodySlots.Contains(b.Slot))
            {
                var err = new ResultResponse();
                err.AddErrorMessage(
                    $"Body has no {{{{{b.Slot}}}}} placeholder for this binding.",
                    "integrations.templates.bindings_unexpected");
                return err;
            }
        }

        foreach (var s in headerSlots)
        {
            if (!bindings.Any(b =>
                    string.Equals(b.Section, "header", StringComparison.OrdinalIgnoreCase) && b.Slot == s))
            {
                var err = new ResultResponse();
                err.AddErrorMessage(
                    $"Missing binding for header placeholder {{{{{s}}}}}.",
                    "integrations.templates.bindings_slot_mismatch");
                return err;
            }
        }

        foreach (var s in bodySlots)
        {
            if (!bindings.Any(b =>
                    string.Equals(b.Section, "body", StringComparison.OrdinalIgnoreCase) && b.Slot == s))
            {
                var err = new ResultResponse();
                err.AddErrorMessage(
                    $"Missing binding for body placeholder {{{{{s}}}}}.",
                    "integrations.templates.bindings_slot_mismatch");
                return err;
            }
        }

        return null;
    }

    /// <summary>Align sources to catalog casing and fill default examples when omitted.</summary>
    public static WhatsAppTemplateVariableBindingDto[] Normalize(IReadOnlyList<WhatsAppTemplateVariableBindingDto> bindings)
    {
        return bindings.Select(b =>
        {
            var src = b.Source.Trim();
            var match = WhatsAppTemplateVariableCatalog.AllowedSources.FirstOrDefault(a =>
                string.Equals(a, src, StringComparison.OrdinalIgnoreCase)) ?? src.ToLowerInvariant();
            var ex = string.IsNullOrWhiteSpace(b.Example)
                ? WhatsAppTemplateVariableCatalog.DefaultExampleForSource(match)
                : b.Example.Trim();
            return new WhatsAppTemplateVariableBindingDto
            {
                Slot = b.Slot,
                Source = match,
                Example = ex,
                Section = (b.Section ?? "body").Trim().ToLowerInvariant()
            };
        }).ToArray();
    }
}
