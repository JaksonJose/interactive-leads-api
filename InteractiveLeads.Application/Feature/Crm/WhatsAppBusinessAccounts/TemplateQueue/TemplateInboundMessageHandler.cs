using System.Buffers;
using System.Text;
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
    private const string EventTemplate = "template";
    private const string EventTemplateSynced = "template_synced";

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
            if (!root.TryGetProperty("eventType", out var et))
                return true;

            var eventType = et.GetString()?.Trim();
            if (string.Equals(eventType, EventTemplateSynced, StringComparison.OrdinalIgnoreCase))
                return await HandleTemplateSyncedAsync(root, cancellationToken);

            if (string.Equals(eventType, EventTemplate, StringComparison.OrdinalIgnoreCase))
                return await HandleTemplateAsync(root, cancellationToken);

            logger.LogDebug("Template inbound: ignored eventType {EventType}", eventType);
            return true;
        }
    }

    /// <summary>Status/details for a template row we already have (<c>correlationId</c> = CRM template id from create flow).</summary>
    private async Task<bool> HandleTemplateAsync(JsonElement root, CancellationToken cancellationToken)
    {
        if (!root.TryGetProperty("payload", out var payload))
        {
            logger.LogWarning("Template inbound: missing payload.");
            return true;
        }

        _ = root.TryGetProperty("identifications", out var identifications);

        var template = await ResolveTemplateForLifecycleAsync(identifications, payload, cancellationToken);
        if (template is null)
        {
            logger.LogWarning("Template inbound: no matching template row.");
            return true;
        }

        ApplyMetaPayloadFields(template, payload);
        template.LastSyncedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Sync from Meta (one message per template). Upserts by WABA + name + language; <c>correlationId</c> here is the sync job id, not the template PK.</summary>
    private async Task<bool> HandleTemplateSyncedAsync(JsonElement root, CancellationToken cancellationToken)
    {
        if (!root.TryGetProperty("payload", out var payload))
        {
            logger.LogWarning("Template inbound (template_synced): missing payload.");
            return true;
        }

        _ = root.TryGetProperty("identifications", out var identifications);

        var waba = await ResolveWabaAsync(identifications, cancellationToken);
        if (waba is null)
        {
            logger.LogWarning("Template inbound (template_synced): WABA not found from identifications.");
            return true;
        }

        if (!payload.TryGetProperty("name", out var nameEl))
        {
            logger.LogWarning("Template inbound (template_synced): payload missing name.");
            return true;
        }

        var name = JsonStringOrRaw(nameEl);
        if (string.IsNullOrWhiteSpace(name))
        {
            logger.LogWarning("Template inbound (template_synced): empty template name.");
            return true;
        }

        name = name.Trim();
        var language = ReadOptionalString(payload, "language") ?? string.Empty;

        var template = await db.WhatsAppTemplates
            .FirstOrDefaultAsync(
                t => t.WhatsAppBusinessAccountId == waba.Id && t.Name == name && t.Language == language,
                cancellationToken);

        if (template is null)
        {
            var metaId = ReadMetaTemplateId(payload);
            if (!string.IsNullOrEmpty(metaId))
            {
                template = await db.WhatsAppTemplates
                    .FirstOrDefaultAsync(
                        t => t.WhatsAppBusinessAccountId == waba.Id && t.MetaTemplateId == metaId,
                        cancellationToken);
            }
        }

        var isNew = template is null;
        if (isNew)
        {
            template = new WhatsAppTemplate
            {
                Id = Guid.NewGuid(),
                WhatsAppBusinessAccountId = waba.Id,
                Name = name,
                Language = language,
                Category = string.Empty,
                Status = string.Empty,
                MetaTemplateId = string.Empty,
                ComponentsJson = "{}",
                LastSyncedAt = DateTimeOffset.UtcNow
            };
            db.WhatsAppTemplates.Add(template);
        }

        ApplyMetaPayloadFields(template!, payload);
        template!.ComponentsJson = BuildSyncedComponentsJson(payload);
        template.LastSyncedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            isNew
                ? "Template inbound (template_synced): inserted {Name} ({Language}) for WABA {WabaCrmId}"
                : "Template inbound (template_synced): updated {Name} ({Language}) for WABA {WabaCrmId}",
            name,
            language,
            waba.Id);

        return true;
    }

    private static void ApplyMetaPayloadFields(WhatsAppTemplate template, JsonElement payload)
    {
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
    }

    private static string BuildSyncedComponentsJson(JsonElement payload)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteString("source", "meta_template_sync");
            if (payload.TryGetProperty("components", out var components))
            {
                writer.WritePropertyName("components");
                components.WriteTo(writer);
            }

            CopyScalarIfPresent(writer, payload, "parameter_format", "parameterFormat");
            writer.WriteEndObject();
        }

        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        return string.IsNullOrWhiteSpace(json) ? "{}" : json;
    }

    private static void CopyScalarIfPresent(Utf8JsonWriter writer, JsonElement payload, string payloadProperty, string outputName)
    {
        if (!payload.TryGetProperty(payloadProperty, out var el))
            return;
        writer.WritePropertyName(outputName);
        el.WriteTo(writer);
    }

    private async Task<WhatsAppBusinessAccount?> ResolveWabaAsync(JsonElement identifications, CancellationToken cancellationToken)
    {
        if (identifications.ValueKind == JsonValueKind.Object &&
            TryGetPropertyIgnoreCase(identifications, "whatsAppBusinessAccountId", out var crmWabaEl))
        {
            var s = JsonStringOrRaw(crmWabaEl);
            if (!string.IsNullOrWhiteSpace(s) && Guid.TryParse(s.Trim(), out var wabaGuid))
            {
                return await db.WhatsAppBusinessAccounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == wabaGuid, cancellationToken);
            }
        }

        if (identifications.ValueKind != JsonValueKind.Object ||
            !TryGetPropertyIgnoreCase(identifications, "wabaId", out var wEl))
            return null;

        var metaWabaId = wEl.ValueKind switch
        {
            JsonValueKind.String => wEl.GetString(),
            JsonValueKind.Number => wEl.GetInt64().ToString(),
            _ => wEl.GetRawText().Trim('"')
        };

        if (string.IsNullOrWhiteSpace(metaWabaId))
            return null;

        return await db.WhatsAppBusinessAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WabaId == metaWabaId.Trim(), cancellationToken);
    }

    private static string? ReadMetaTemplateId(JsonElement payload)
    {
        if (!payload.TryGetProperty("id", out var idProp))
            return null;
        return idProp.ValueKind == JsonValueKind.Number
            ? idProp.GetInt64().ToString()
            : (idProp.GetString() ?? string.Empty).Trim();
    }

    private static string? ReadOptionalString(JsonElement payload, string name)
    {
        if (!payload.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String)
            return null;
        return el.GetString()?.Trim();
    }

    private async Task<WhatsAppTemplate?> ResolveTemplateForLifecycleAsync(
        JsonElement identifications,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        string? correlationId = null;
        if (identifications.ValueKind == JsonValueKind.Object &&
            TryGetPropertyIgnoreCase(identifications, "correlationId", out var cEl))
        {
            correlationId = JsonStringOrRaw(cEl);
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
            TryGetPropertyIgnoreCase(identifications, "wabaId", out var wEl))
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

        var name = JsonStringOrRaw(nameEl);
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var waba = await db.WhatsAppBusinessAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WabaId == wabaId, cancellationToken);
        if (waba is null)
            return null;

        var q = db.WhatsAppTemplates.Where(t =>
            t.WhatsAppBusinessAccountId == waba.Id && t.Name == name.Trim());

        if (payload.TryGetProperty("language", out var langEl) && langEl.ValueKind == JsonValueKind.String)
        {
            var language = langEl.GetString();
            if (!string.IsNullOrWhiteSpace(language))
                q = q.Where(t => t.Language == language);
        }

        return await q.FirstOrDefaultAsync(cancellationToken);
    }

    private static string JsonStringOrRaw(JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.String => el.GetString() ?? string.Empty,
            _ => el.GetRawText().Trim('"')
        };

    private static bool TryGetPropertyIgnoreCase(JsonElement obj, string name, out JsonElement value)
    {
        foreach (var p in obj.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = p.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
