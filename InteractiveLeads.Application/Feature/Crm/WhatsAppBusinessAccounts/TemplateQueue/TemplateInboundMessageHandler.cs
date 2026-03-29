using System.Text.Json;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.TemplateQueue;

public sealed class TemplateInboundMessageHandler(
    IApplicationDbContext db,
    ILogger<TemplateInboundMessageHandler> logger) : ITemplateInboundMessageHandler
{
    public async Task<bool> TryHandleAsync(string jsonBody, CancellationToken cancellationToken)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(jsonBody);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Template inbound: invalid JSON.");
            return true;
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (!root.TryGetProperty("eventType", out var et) ||
                !string.Equals(et.GetString(), "template", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!root.TryGetProperty("payload", out var payload))
            {
                logger.LogWarning("Template inbound: missing payload.");
                return true;
            }

            _ = root.TryGetProperty("identifications", out var identifications);

            var template = await ResolveTemplateAsync(identifications, payload, cancellationToken);
            if (template is null)
            {
                logger.LogWarning("Template inbound: no matching template row.");
                return true;
            }

            if (payload.TryGetProperty("id", out var idProp))
            {
                template.MetaTemplateId = idProp.ValueKind == JsonValueKind.Number
                    ? idProp.GetInt64().ToString()
                    : (idProp.GetString() ?? string.Empty);
            }

            if (payload.TryGetProperty("status", out var st) && st.ValueKind == JsonValueKind.String)
            {
                var s = st.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                    template.Status = s.Trim();
            }

            if (payload.TryGetProperty("category", out var cat) && cat.ValueKind == JsonValueKind.String)
            {
                var c = cat.GetString();
                if (!string.IsNullOrWhiteSpace(c))
                    template.Category = c.Trim().ToUpperInvariant();
            }

            if (payload.TryGetProperty("language", out var lang) && lang.ValueKind == JsonValueKind.String)
            {
                var l = lang.GetString();
                if (!string.IsNullOrWhiteSpace(l))
                    template.Language = l.Trim();
            }

            template.LastSyncedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    private async Task<WhatsAppTemplate?> ResolveTemplateAsync(
        JsonElement identifications,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        string? correlationId = null;
        if (identifications.ValueKind == JsonValueKind.Object &&
            identifications.TryGetProperty("correlationId", out var cEl))
        {
            correlationId = cEl.ValueKind switch
            {
                JsonValueKind.String => cEl.GetString(),
                _ => cEl.GetRawText().Trim('"')
            };
        }

        if (!string.IsNullOrWhiteSpace(correlationId) && Guid.TryParse(correlationId, out var gid))
        {
            var byId = await db.WhatsAppTemplates
                .FirstOrDefaultAsync(t => t.Id == gid, cancellationToken);
            if (byId != null)
                return byId;
        }

        string? wabaId = null;
        if (identifications.ValueKind == JsonValueKind.Object &&
            identifications.TryGetProperty("wabaId", out var wEl))
        {
            wabaId = wEl.ValueKind switch
            {
                JsonValueKind.String => wEl.GetString(),
                JsonValueKind.Number => wEl.GetInt64().ToString(),
                _ => wEl.ToString()
            };
        }

        if (string.IsNullOrWhiteSpace(wabaId) ||
            !payload.TryGetProperty("name", out var nameEl))
            return null;

        var name = nameEl.ValueKind == JsonValueKind.String
            ? nameEl.GetString()
            : nameEl.GetRawText().Trim('"');
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var waba = await db.WhatsAppBusinessAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WabaId == wabaId, cancellationToken);
        if (waba is null)
            return null;

        var q = db.WhatsAppTemplates.Where(t =>
            t.WhatsAppBusinessAccountId == waba.Id && t.Name == name);

        if (payload.TryGetProperty("language", out var langEl) && langEl.ValueKind == JsonValueKind.String)
        {
            var language = langEl.GetString();
            if (!string.IsNullOrWhiteSpace(language))
                q = q.Where(t => t.Language == language);
        }

        return await q.FirstOrDefaultAsync(cancellationToken);
    }
}
