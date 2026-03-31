using System.Text.RegularExpressions;
using InteractiveLeads.Application.Responses;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

/// <summary>
/// Converts CRM semantic placeholders (<c>{{contact.name}}</c>) into Meta positional placeholders (<c>{{1}}</c>),
/// or validates legacy numeric-only placeholders. Header and body are compiled separately.
/// </summary>
public static partial class WhatsAppTemplatePlaceholderCompiler
{
    [GeneratedRegex(@"\{\{([^{}]+)\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();

    [GeneratedRegex(@"^\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex NumericTokenRegex();

    [GeneratedRegex(@"^[a-z_][a-z0-9_.]*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SemanticTokenRegex();

    /// <summary>
    /// Compile header (optional) and body. Uses <paramref name="authoringHeader"/> and <paramref name="authoringBody"/> when provided;
    /// otherwise falls back to <paramref name="headerText"/> and <paramref name="body"/>.
    /// </summary>
    public static ResultResponse? TryCompileAndApply(
        ref string? headerText,
        ref string body,
        ref string? headerExample,
        ref string[]? bodyExamples,
        string? authoringHeader,
        string? authoringBody,
        out string? persistedAuthoringHeader,
        out string persistedAuthoringBody,
        out IReadOnlyList<WhatsAppTemplateVariableBindingDto> allBindings)
    {
        var authHeader = !string.IsNullOrWhiteSpace(authoringHeader)
            ? authoringHeader.Trim()
            : headerText?.Trim();
        var authBody = !string.IsNullOrWhiteSpace(authoringBody)
            ? authoringBody.Trim()
            : body.Trim();
        persistedAuthoringBody = authBody;
        persistedAuthoringHeader = string.IsNullOrWhiteSpace(authHeader) ? null : authHeader;

        var bindings = new List<WhatsAppTemplateVariableBindingDto>();
        string? metaHeader = null;
        string? metaHeaderExample = null;
        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            var hr = CompileSection(authHeader, "header", WhatsAppTemplateVariableCatalog.MaxHeaderNumericSlots);
            if (hr.Error != null)
            {
                allBindings = [];
                return hr.Error;
            }

            metaHeader = hr.MetaText;
            metaHeaderExample = hr.HeaderExample;
            bindings.AddRange(hr.Bindings);
        }

        var br = CompileSection(authBody, "body", WhatsAppTemplateVariableCatalog.MaxBodyNumericSlots);
        if (br.Error != null)
        {
            allBindings = [];
            return br.Error;
        }

        bindings.AddRange(br.Bindings);
        allBindings = bindings;

        headerText = metaHeader;
        body = br.MetaText;
        if (metaHeaderExample != null)
            headerExample = metaHeaderExample;

        if (br.BodyExampleRow.Count > 0)
            bodyExamples = br.BodyExampleRow.ToArray();

        var layoutHeaderErr = ValidateCompiledMetaLayout(metaHeader, section: "header");
        if (layoutHeaderErr != null)
        {
            allBindings = [];
            return layoutHeaderErr;
        }

        var layoutBodyErr = ValidateCompiledMetaLayout(body, section: "body");
        if (layoutBodyErr != null)
        {
            allBindings = [];
            return layoutBodyErr;
        }

        return null;
    }

    /// <summary>
    /// WhatsApp Manager rules (body/header with positional <c>{{n}}</c>): placeholders must not touch the start or end of the trimmed
    /// text; the body must also have enough length vs variable count. Meta does not document an exact formula —
    /// ratios here are conservative approximations to surface errors before submission.
    /// </summary>
    private static ResultResponse? ValidateCompiledMetaLayout(string? metaText, string section)
    {
        if (string.IsNullOrWhiteSpace(metaText))
            return null;

        var t = metaText.Trim();
        var matches = PlaceholderRegex().Matches(t);
        if (matches.Count == 0)
            return null;

        if (matches[0].Index == 0)
        {
            var err = new ResultResponse();
            err.AddErrorMessage(
                section == "header"
                    ? "Variables cannot be at the beginning or end of the header."
                    : "Variables cannot be at the beginning or end of the body.",
                section == "header"
                    ? "integrations.templates.placeholder_header_edges"
                    : "integrations.templates.placeholder_body_edges");
            return err;
        }

        var last = matches[^1];
        if (last.Index + last.Length == t.Length)
        {
            var err = new ResultResponse();
            err.AddErrorMessage(
                section == "header"
                    ? "Variables cannot be at the beginning or end of the header."
                    : "Variables cannot be at the beginning or end of the body.",
                section == "header"
                    ? "integrations.templates.placeholder_header_edges"
                    : "integrations.templates.placeholder_body_edges");
            return err;
        }

        if (section != "body")
            return null;

        var varCount = ParseMetaSlots(t).Count;
        if (varCount <= 0)
            return null;

        var staticText = PlaceholderRegex().Replace(t, string.Empty).Trim();
        var staticLen = staticText.Length;
        const int minStaticCharsPerVariable = 3;
        if (staticLen < varCount * minStaticCharsPerVariable)
        {
            var err = new ResultResponse();
            err.AddErrorMessage(
                "This template has too many variables for the body length. Add more text or use fewer variables.",
                "integrations.templates.variables_density_body");
            return err;
        }

        const int minTotalCharsPerBodyVariable = 9;
        if (t.Length < varCount * minTotalCharsPerBodyVariable)
        {
            var err = new ResultResponse();
            err.AddErrorMessage(
                "This template has too many variables for the body length. Add more text or use fewer variables.",
                "integrations.templates.variables_density_body");
            return err;
        }

        return null;
    }

    private sealed record SectionCompile(string MetaText, List<WhatsAppTemplateVariableBindingDto> Bindings, string? HeaderExample, List<string> BodyExampleRow, ResultResponse? Error);

    private static SectionCompile CompileSection(string text, string section, int maxNumericSlots)
    {
        var matches = PlaceholderRegex().Matches(text);
        if (matches.Count == 0)
            return new SectionCompile(text, [], null, [], null);

        var tokens = matches.Select(m => m.Groups[1].Value.Trim()).ToList();
        var hasNumeric = tokens.Any(t => NumericTokenRegex().IsMatch(t));
        var hasSemantic = tokens.Any(t => !NumericTokenRegex().IsMatch(t));

        if (hasNumeric && hasSemantic)
        {
            var err = new ResultResponse();
            err.AddErrorMessage(
                "Cannot mix numeric {{1}} placeholders with semantic {{contact.name}} in the same field.",
                "integrations.templates.placeholders_mixed");
            return new SectionCompile(text, [], null, [], err);
        }

        if (hasNumeric)
            return CompileNumericSection(text, section, maxNumericSlots);

        return CompileSemanticSection(text, section);
    }

    private static SectionCompile CompileNumericSection(string text, string section, int maxSlots)
    {
        var matches = PlaceholderRegex().Matches(text);
        var nums = new List<int>();
        foreach (Match m in matches)
        {
            if (!int.TryParse(m.Groups[1].Value.Trim(), out var n) || n < 1)
            {
                var err = new ResultResponse();
                err.AddErrorMessage("Invalid numeric placeholder.", "integrations.templates.placeholder_invalid");
                return new SectionCompile(text, [], null, [], err);
            }
            nums.Add(n);
        }

        var distinct = nums.Distinct().OrderBy(x => x).ToList();
        if (distinct.Count == 0)
            return new SectionCompile(text, [], null, [], null);

        if (distinct[0] != 1 || distinct[^1] != distinct.Count)
        {
            var err = new ResultResponse();
            err.AddErrorMessage(
                "Numeric placeholders must be contiguous starting at {{1}} (e.g. {{1}}, {{2}}, …) with no gaps.",
                "integrations.templates.placeholders_not_contiguous");
            return new SectionCompile(text, [], null, [], err);
        }

        if (distinct.Count > maxSlots)
        {
            var err = new ResultResponse();
            err.AddErrorMessage("Too many variables in this section.", "integrations.templates.too_many_variables");
            return new SectionCompile(text, [], null, [], err);
        }

        if (section == "header" && distinct.Count > WhatsAppTemplateVariableCatalog.MaxHeaderNumericSlots)
        {
            var err = new ResultResponse();
            err.AddErrorMessage("Header supports at most one variable.", "integrations.templates.header_too_many_variables");
            return new SectionCompile(text, [], null, [], err);
        }

        // Legacy numeric: no semantic bindings until user configures via PATCH
        return new SectionCompile(text, [], null, [], null);
    }

    private static SectionCompile CompileSemanticSection(string text, string section)
    {
        var matches = PlaceholderRegex().Matches(text);
        var orderedUnique = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in matches)
        {
            var raw = m.Groups[1].Value.Trim();
            if (!SemanticTokenRegex().IsMatch(raw))
            {
                var err = new ResultResponse();
                err.AddErrorMessage($"Invalid placeholder token: {{{{{raw}}}}}", "integrations.templates.placeholder_invalid");
                return new SectionCompile(text, [], null, [], err);
            }

            if (!WhatsAppTemplateVariableCatalog.IsAllowedSource(raw))
            {
                var err = new ResultResponse();
                err.AddErrorMessage($"Unknown variable: {{{{{raw}}}}}", "integrations.templates.unknown_variable");
                return new SectionCompile(text, [], null, [], err);
            }

            var key = raw.Trim().ToLowerInvariant();
            if (seen.Add(key))
                orderedUnique.Add(key);
        }

        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var bindings = new List<WhatsAppTemplateVariableBindingDto>();
        for (var i = 0; i < orderedUnique.Count; i++)
        {
            var slot = i + 1;
            var src = orderedUnique[i];
            map[src] = slot;
            var example = WhatsAppTemplateVariableCatalog.DefaultExampleForSource(src);
            bindings.Add(new WhatsAppTemplateVariableBindingDto
            {
                Slot = slot,
                Source = src,
                Example = example,
                Section = section
            });
        }

        if (section == "header" && bindings.Count > WhatsAppTemplateVariableCatalog.MaxHeaderNumericSlots)
        {
            var err = new ResultResponse();
            err.AddErrorMessage("Header supports at most one variable.", "integrations.templates.header_too_many_variables");
            return new SectionCompile(text, [], null, [], err);
        }

        if (bindings.Count > WhatsAppTemplateVariableCatalog.MaxBodyNumericSlots && section == "body")
        {
            var err = new ResultResponse();
            err.AddErrorMessage("Too many variables in body.", "integrations.templates.too_many_variables");
            return new SectionCompile(text, [], null, [], err);
        }

        var meta = PlaceholderRegex().Replace(text, m =>
        {
            var raw = m.Groups[1].Value.Trim();
            var key = raw.ToLowerInvariant();
            var slot = map[key];
            return $"{{{{{slot}}}}}";
        });

        string? headerExample = null;
        var bodyRow = new List<string>();
        if (section == "header" && bindings.Count == 1)
            headerExample = bindings[0].Example;
        else if (section == "body" && bindings.Count > 0)
            bodyRow.AddRange(bindings.Select(b => b.Example ?? "—"));

        return new SectionCompile(meta, bindings, headerExample, bodyRow, null);
    }

    /// <summary>Parse Meta-style <c>{{n}}</c> placeholders; returns sorted unique 1-based indices.</summary>
    public static IReadOnlyList<int> ParseMetaSlots(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];
        var set = new SortedSet<int>();
        foreach (Match m in PlaceholderRegex().Matches(text))
        {
            if (int.TryParse(m.Groups[1].Value.Trim(), out var n) && n >= 1)
                set.Add(n);
        }
        return set.ToList();
    }

    /// <summary>Whether the text contains only numeric placeholders (legacy Meta authoring).</summary>
    public static bool ContainsOnlyNumericPlaceholders(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        var matches = PlaceholderRegex().Matches(text);
        if (matches.Count == 0)
            return false;
        return matches.Cast<Match>().All(m => NumericTokenRegex().IsMatch(m.Groups[1].Value.Trim()));
    }

    /// <summary>Whether the text contains any semantic (non-numeric) placeholder.</summary>
    public static bool ContainsSemanticPlaceholders(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        return PlaceholderRegex().Matches(text).Cast<Match>()
            .Any(m => !NumericTokenRegex().IsMatch(m.Groups[1].Value.Trim()));
    }
}
