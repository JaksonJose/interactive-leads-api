using System.Text.RegularExpressions;
using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

namespace InteractiveLeads.Application.Feature.Chat.Messages;

public static partial class TemplateMessageSnapshotBuilder
{
    [GeneratedRegex(@"\{\{(\d+)\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex SlotRegex();

    public static string ApplyNumericPlaceholders(string? text, IReadOnlyList<string> values)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return SlotRegex().Replace(text, m =>
        {
            if (!int.TryParse(m.Groups[1].Value, out var n) || n < 1)
                return m.Value;
            var idx = n - 1;
            if (idx >= values.Count)
                return string.Empty;
            return values[idx] ?? string.Empty;
        });
    }

    public static MessageTemplateSnapshotDto BuildSnapshot(
        WhatsAppTemplateDetailDto detail,
        string? headerParameter,
        IReadOnlyList<string> bodyParameters)
    {
        var headerSlots = WhatsAppTemplatePlaceholderCompiler.ParseMetaSlots(detail.HeaderText).Count;
        var bodySlots = WhatsAppTemplatePlaceholderCompiler.ParseMetaSlots(detail.Body).Count;

        string? headerOut = null;
        if (!string.IsNullOrWhiteSpace(detail.HeaderText))
        {
            var h = detail.HeaderText.Trim();
            headerOut = headerSlots > 0
                ? ApplyNumericPlaceholders(h, headerParameter != null ? new[] { headerParameter } : [])
                : h;
        }

        var bodyRaw = detail.Body ?? string.Empty;
        var bodyOut = bodySlots > 0
            ? ApplyNumericPlaceholders(bodyRaw, bodyParameters)
            : bodyRaw.Trim();

        var footerOut = string.IsNullOrWhiteSpace(detail.Footer) ? null : detail.Footer.Trim();

        IReadOnlyList<MessageTemplateButtonSnapshotDto>? buttons = null;
        if (detail.Buttons is { Length: > 0 })
        {
            var list = detail.Buttons
                .Where(b => !string.IsNullOrWhiteSpace(b.Text))
                .Select(b => new MessageTemplateButtonSnapshotDto
                {
                    Type = b.Type,
                    Text = b.Text.Trim(),
                    Url = string.IsNullOrWhiteSpace(b.Url) ? null : b.Url.Trim(),
                    PhoneNumber = string.IsNullOrWhiteSpace(b.PhoneNumber) ? null : b.PhoneNumber.Trim()
                })
                .ToList();
            if (list.Count > 0)
                buttons = list;
        }

        return new MessageTemplateSnapshotDto
        {
            HeaderText = headerOut,
            BodyText = bodyOut,
            FooterText = footerOut,
            Buttons = buttons
        };
    }
}
