using System.Buffers;
using System.Text;
using System.Text.Json;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.TemplateQueue;

/// <summary>
/// Handles <c>template</c> lifecycle replies (create/delete outcomes) and <c>template_synced</c> from Meta.
/// For create failures, the worker should POST <c>eventType: update_template</c> with <c>identifications.correlationId</c> = CRM template id
/// and a payload such as: <c>success: false</c>, <c>error: "text"</c>, or <c>error: { "message", "code" }</c> (Graph API shapes with <c>error_user_msg</c> are recognized).
/// </summary>
public sealed class TemplateInboundMessageHandler(
    IApplicationDbContext db,
    ILogger<TemplateInboundMessageHandler> logger) : ITemplateInboundMessageHandler
{
    /// <summary>
    /// Template lifecycle/status reply (create/delete outcomes). New preferred name is <c>update_template</c>.
    /// </summary>
    private const string EventUpdateTemplate = "update_template";
    private const string EventTemplateLegacy = "template";
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
            string? eventType = null;
            if (root.TryGetProperty("eventType", out var et))
                eventType = JsonStringOrRaw(et).Trim();

            if (string.IsNullOrEmpty(eventType) && root.TryGetProperty("payload", out var inferPayload) &&
                LooksLikeTemplateLifecycleReply(inferPayload))
            {
                eventType = EventUpdateTemplate;
                logger.LogInformation(
                    "Template inbound: inferred eventType \"{Event}\" (payload has template correlationId + failure signals).",
                    EventUpdateTemplate);
            }

            if (string.IsNullOrEmpty(eventType))
            {
                logger.LogWarning(
                    "Template inbound: missing/empty eventType — expected \"{UpdateTemplate}\" or \"{Synced}\". Message was ACKed but not applied. Enable RabbitMQ on the API host and publish to queue {QueueHint} (see appsettings RabbitMq:TemplateInboundQueueName).",
                    EventUpdateTemplate,
                    EventTemplateSynced,
                    "interactive-template-inbound");
                return true;
            }

            if (string.Equals(eventType, EventTemplateSynced, StringComparison.OrdinalIgnoreCase))
                return await HandleTemplateSyncedAsync(root, cancellationToken);

            if (string.Equals(eventType, EventUpdateTemplate, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(eventType, EventTemplateLegacy, StringComparison.OrdinalIgnoreCase))
                return await HandleTemplateAsync(root, cancellationToken);

            logger.LogWarning("Template inbound: ignored unsupported eventType {EventType}", eventType);
            return true;
        }
    }

    /// <summary>
    /// Workers sometimes omit <c>eventType</c>. If the payload clearly refers to a CRM template row
    /// (<c>correlationId</c> = template PK) plus a failure signal, treat as <c>template</c> lifecycle.
    /// </summary>
    private static bool LooksLikeTemplateLifecycleReply(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
            return false;

        if (!payload.TryGetProperty("correlationId", out var cEl))
            return false;

        var corr = JsonStringOrRaw(cEl).Trim();
        if (string.IsNullOrWhiteSpace(corr) || !Guid.TryParse(corr, out _))
            return false;

        if (payload.TryGetProperty("error", out var err) &&
            err.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
            return true;

        if (payload.TryGetProperty("success", out var suc) && suc.ValueKind == JsonValueKind.False)
            return true;

        if (payload.TryGetProperty("status", out var st) && st.ValueKind == JsonValueKind.String)
        {
            var s = st.GetString();
            if (!string.IsNullOrWhiteSpace(s) && s.Trim().Equals("failed", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>Status/details for a template row we already have (<c>correlationId</c> = CRM template id from create flow).</summary>
    private async Task<bool> HandleTemplateAsync(JsonElement root, CancellationToken cancellationToken)
    {
        if (!root.TryGetProperty("payload", out var payload))
        {
            logger.LogWarning("Template inbound: missing payload.");
            return true;
        }

        _ = root.TryGetProperty("identifications", out var identifications) ||
            root.TryGetProperty("indentifications", out identifications);

        var template = await ResolveTemplateForLifecycleAsync(identifications, payload, cancellationToken);
        if (template is null)
        {
            logger.LogWarning(
                "Template inbound: no matching template row. Ensure identifications/payload carries CRM template id as correlationId (same value returned by create-template API), or identifications.wabaId + payload.name (+ language).");
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

        _ = root.TryGetProperty("identifications", out var identifications) ||
            root.TryGetProperty("indentifications", out identifications);

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
        var errorApplied = TryApplySubmissionErrorFromPayload(template, payload);

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

        if (!string.IsNullOrWhiteSpace(template.MetaTemplateId))
        {
            template.SubmissionLastError = null;
            template.SubmissionLastErrorCode = null;
            template.SubmissionLastErrorAt = null;
        }
        else if (errorApplied)
            template.Status = "FAILED";
    }

    private static bool TryApplySubmissionErrorFromPayload(WhatsAppTemplate template, JsonElement payload)
    {
        string? message = null;
        string? code = null;

        if (payload.TryGetProperty("success", out var suc) && suc.ValueKind == JsonValueKind.False)
            message = "Template submission was rejected by WhatsApp (Meta).";

        // Accept Graph-ish shapes:
        // - payload.error: "text"
        // - payload.error: { message, code, error_user_msg, error_subcode, ... }
        // - payload.error: { error: { message, code, ... } }  (some workers wrap it)
        // - payload.errorMessage: { error: { ... } }          (legacy worker naming)
        if (!payload.TryGetProperty("error", out var errEl) &&
            payload.TryGetProperty("errorMessage", out var legacyErr))
        {
            errEl = legacyErr;
        }

        if (errEl.ValueKind != JsonValueKind.Undefined)
        {
            if (errEl.ValueKind == JsonValueKind.String)
            {
                var s = errEl.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                    message = s.Trim();
            }
            else if (errEl.ValueKind == JsonValueKind.Object)
            {
                if (errEl.TryGetProperty("error", out var nestedErr) && nestedErr.ValueKind == JsonValueKind.Object)
                    errEl = nestedErr;

                message = ReadOptionalString(errEl, "message")
                    ?? ReadOptionalString(errEl, "error_user_msg")
                    ?? ReadOptionalString(errEl, "error_user_title");
                code = ReadJsonScalarAsTrimmedString(errEl, "code")
                    ?? ReadJsonScalarAsTrimmedString(errEl, "error_subcode");
                if (string.IsNullOrWhiteSpace(message) && errEl.TryGetProperty("error", out var nested) &&
                    nested.ValueKind == JsonValueKind.Object)
                {
                    message = ReadOptionalString(nested, "message")
                        ?? ReadOptionalString(nested, "error_user_msg");
                    code ??= ReadJsonScalarAsTrimmedString(nested, "code")
                        ?? ReadJsonScalarAsTrimmedString(nested, "error_subcode");
                }

                if (string.IsNullOrWhiteSpace(message))
                    message = errEl.GetRawText();
            }
        }

        if (string.IsNullOrWhiteSpace(message))
            return false;

        template.SubmissionLastError = Truncate(message.Trim(), 2000);
        template.SubmissionLastErrorCode = string.IsNullOrWhiteSpace(code) ? null : Truncate(code.Trim(), 128);
        template.SubmissionLastErrorAt = DateTimeOffset.UtcNow;
        return true;
    }

    private static string Truncate(string s, int maxLen) => s.Length <= maxLen ? s : s[..maxLen];

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
            !(TryGetPropertyIgnoreCase(identifications, "wabaId", out var wEl) ||
              TryGetPropertyIgnoreCase(identifications, "businessAccountId", out wEl)))
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

    /// <summary>Graph often returns <c>code</c> / <c>error_subcode</c> as numbers.</summary>
    private static string? ReadJsonScalarAsTrimmedString(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var el))
            return null;
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString()?.Trim(),
            JsonValueKind.Number => el.TryGetInt64(out var n) ? n.ToString(System.Globalization.CultureInfo.InvariantCulture) : el.GetRawText().Trim(),
            _ => null
        };
    }

    private async Task<WhatsAppTemplate?> ResolveTemplateForLifecycleAsync(
        JsonElement identifications,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        string? correlationId = null;
        if (identifications.ValueKind == JsonValueKind.Object &&
            TryGetPropertyIgnoreCase(identifications, "correlationId", out var cEl))
            correlationId = JsonStringOrRaw(cEl);

        // Some workers send correlationId under payload; accept it too.
        if (string.IsNullOrWhiteSpace(correlationId) &&
            payload.ValueKind == JsonValueKind.Object &&
            payload.TryGetProperty("correlationId", out var pCorr))
            correlationId = JsonStringOrRaw(pCorr);

        if (!string.IsNullOrWhiteSpace(correlationId) && Guid.TryParse(correlationId.Trim(), out var gid))
        {
            var byId = await db.WhatsAppTemplates
                .FirstOrDefaultAsync(t => t.Id == gid, cancellationToken);
            if (byId != null)
                return byId;
            logger.LogWarning("Template inbound: correlationId {CorrelationId} is not a WhatsAppTemplate primary key in this database.", correlationId);
        }

        string? wabaId = null;
        if (identifications.ValueKind == JsonValueKind.Object &&
            (TryGetPropertyIgnoreCase(identifications, "wabaId", out var wEl) ||
             TryGetPropertyIgnoreCase(identifications, "businessAccountId", out wEl)))
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

        if ((payload.TryGetProperty("language", out var langEl) ||
             payload.TryGetProperty("languague", out langEl)) && langEl.ValueKind == JsonValueKind.String)
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
