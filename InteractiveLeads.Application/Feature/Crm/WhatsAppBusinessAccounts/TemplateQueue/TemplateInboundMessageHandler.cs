using System.Buffers;
using System.Text;
using System.Text.Json;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.TemplateQueue;

/// <summary>
/// Handles <c>template</c> lifecycle replies (create/delete outcomes), explicit <c>delete_template</c> replies, and <c>template_synced</c> from Meta.
/// For create failures, the worker should POST <c>eventType: update_template</c> with <c>identifications.correlationId</c> = CRM template id
/// and a payload such as: <c>success: false</c>, <c>error: "text"</c>, or <c>error: { "message", "code" }</c> (Graph API shapes with <c>error_user_msg</c> are recognized).
/// For delete outcomes, prefer <c>eventType: delete_template</c> with the same correlation id and <c>payload.deleted: true</c> on success.
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
    /// <summary>Explicit delete outcome from worker (preferred for success/failure after <c>delete_template</c> outbound).</summary>
    private const string EventDeleteTemplate = "delete_template";

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
                    "Template inbound: missing/empty eventType — expected \"{UpdateTemplate}\", \"{DeleteTemplate}\", or \"{Synced}\". Message was ACKed but not applied. Enable RabbitMQ on the API host and publish to queue {QueueHint} (see appsettings RabbitMq:TemplateInboundQueueName).",
                    EventUpdateTemplate,
                    EventDeleteTemplate,
                    EventTemplateSynced,
                    "interactive-template-inbound");
                return true;
            }

            if (string.Equals(eventType, EventTemplateSynced, StringComparison.OrdinalIgnoreCase))
                return await HandleTemplateSyncedAsync(root, cancellationToken);

            if (string.Equals(eventType, EventDeleteTemplate, StringComparison.OrdinalIgnoreCase))
                return await HandleTemplateAsync(root, cancellationToken, deleteReplySemantics: true);

            if (string.Equals(eventType, EventUpdateTemplate, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(eventType, EventTemplateLegacy, StringComparison.OrdinalIgnoreCase))
                return await HandleTemplateAsync(root, cancellationToken, deleteReplySemantics: false);

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
    private async Task<bool> HandleTemplateAsync(
        JsonElement root,
        CancellationToken cancellationToken,
        bool deleteReplySemantics = false)
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

        // If this reply corresponds to an async delete request, either delete the row (on success)
        // or keep it disabled and record the delete error (on failure).
        if (deleteReplySemantics || IsDeleteReply(template, payload))
        {
            var deleteOk = deleteReplySemantics
                ? IsDeleteConfirmationSuccess(payload)
                : IsSuccessReply(payload);
            if (deleteOk)
            {
                // ExecuteDelete avoids change-tracker edge cases and removes regardless of IsDisabled / errors on the row.
                var templateId = template.Id;
                var removed = await db.WhatsAppTemplates
                    .Where(t => t.Id == templateId)
                    .ExecuteDeleteAsync(cancellationToken);
                if (removed == 0)
                {
                    logger.LogWarning(
                        "Template inbound: delete confirmed but no row was removed (already gone?). templateId={TemplateId}",
                        templateId);
                }
                else
                {
                    logger.LogInformation("Template inbound: delete succeeded; removed template {TemplateId}", templateId);
                }

                return true;
            }

            var (errMsg, errCode) = ReadErrorFromPayload(payload);
            template.IsDisabled = true;
            template.DisabledAt ??= DateTimeOffset.UtcNow;
            template.DisabledReason = "delete_failed";
            template.DeletePending = false;
            template.DeleteLastError = string.IsNullOrWhiteSpace(errMsg) ? "Delete failed." : Truncate(errMsg.Trim(), 2000);
            template.DeleteLastErrorCode = string.IsNullOrWhiteSpace(errCode) ? null : Truncate(errCode.Trim(), 128);
            template.DeleteLastErrorAt = DateTimeOffset.UtcNow;
            template.LastSyncedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning(
                "Template inbound: delete failed; template kept disabled. templateId={TemplateId} code={Code}",
                template.Id,
                template.DeleteLastErrorCode);
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

        if (errorApplied)
            template.Status = "FAILED";
        else if (!string.IsNullOrWhiteSpace(template.MetaTemplateId))
        {
            template.SubmissionLastError = null;
            template.SubmissionLastErrorCode = null;
            template.SubmissionLastErrorAt = null;
        }
    }

    private static bool TryApplySubmissionErrorFromPayload(WhatsAppTemplate template, JsonElement payload)
    {
        if (payload.TryGetProperty("success", out var suc) && suc.ValueKind == JsonValueKind.True)
            return false;

        if (!LooksLikeTemplateLifecycleReply(payload))
            return false;

        var (message, code) = ReadErrorFromPayload(payload);
        if (string.IsNullOrWhiteSpace(message))
            message = "Template submission was rejected by WhatsApp (Meta).";

        template.SubmissionLastError = Truncate(message.Trim(), 2000);
        template.SubmissionLastErrorCode = string.IsNullOrWhiteSpace(code) ? null : Truncate(code.Trim(), 128);
        template.SubmissionLastErrorAt = DateTimeOffset.UtcNow;
        return true;
    }

    private static string Truncate(string s, int maxLen) => s.Length <= maxLen ? s : s[..maxLen];

    private static bool IsDeleteReply(WhatsAppTemplate template, JsonElement payload)
    {
        // Some workers return "deleted: true" without additional operation fields.
        // Treat it as a delete lifecycle reply regardless of local DeletePending flag.
        if (payload.ValueKind == JsonValueKind.Object &&
            payload.TryGetProperty("deleted", out var del) &&
            del.ValueKind is JsonValueKind.True)
            return true;

        if (template.DeletePending)
            return true;

        var op = ReadOptionalString(payload, "operation")
                 ?? ReadOptionalString(payload, "action")
                 ?? ReadOptionalString(payload, "eventType");
        return !string.IsNullOrWhiteSpace(op) &&
               (op.Equals("delete", StringComparison.OrdinalIgnoreCase) ||
                op.Equals("delete_template", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Success semantics for <c>eventType: delete_template</c>: explicit failure loses; if there is no explicit failure,
    /// treat as confirmed delete (workers often send only name/id/<c>deleted</c>).
    /// </summary>
    private static bool IsDeleteConfirmationSuccess(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
            return false;

        if (payload.TryGetProperty("success", out var suc0) && IsJsonExplicitFalse(suc0))
            return false;

        if (payload.TryGetProperty("deleted", out var del0) && IsJsonExplicitFalse(del0))
            return false;

        if (payload.TryGetProperty("status", out var st0) && st0.ValueKind == JsonValueKind.String)
        {
            var s0 = st0.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(s0) && s0.Equals("failed", StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (payload.TryGetProperty("error", out var err) && err.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            if (!IsEffectivelyEmptyJsonError(err))
                return false;
        }

        if (payload.TryGetProperty("success", out var suc) && IsJsonExplicitTrue(suc))
            return true;

        if (payload.TryGetProperty("deleted", out var del) && IsJsonExplicitTrue(del))
            return true;

        if (payload.TryGetProperty("status", out var st) && st.ValueKind == JsonValueKind.String)
        {
            var s = st.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(s) &&
                (s.Equals("success", StringComparison.OrdinalIgnoreCase) ||
                 s.Equals("deleted", StringComparison.OrdinalIgnoreCase) ||
                 s.Equals("ok", StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return true;
    }

    private static bool IsJsonExplicitFalse(JsonElement el) =>
        el.ValueKind == JsonValueKind.False ||
        (el.ValueKind == JsonValueKind.String &&
         string.Equals(el.GetString()?.Trim(), "false", StringComparison.OrdinalIgnoreCase));

    private static bool IsJsonExplicitTrue(JsonElement el) =>
        el.ValueKind == JsonValueKind.True ||
        (el.ValueKind == JsonValueKind.String &&
         string.Equals(el.GetString()?.Trim(), "true", StringComparison.OrdinalIgnoreCase)) ||
        (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n) && n == 1);

    /// <summary>True when <paramref name="err"/> is empty object, empty array, or whitespace string — not a real Graph error.</summary>
    private static bool IsEffectivelyEmptyJsonError(JsonElement err)
    {
        return err.ValueKind switch
        {
            JsonValueKind.Object => !err.EnumerateObject().Any(),
            JsonValueKind.Array => err.GetArrayLength() == 0,
            JsonValueKind.String => string.IsNullOrWhiteSpace(err.GetString()),
            _ => false
        };
    }

    private static bool IsSuccessReply(JsonElement payload)
    {
        if (payload.TryGetProperty("success", out var suc) && suc.ValueKind is JsonValueKind.True)
            return true;

        if (payload.TryGetProperty("deleted", out var del) && del.ValueKind is JsonValueKind.True)
            return true;

        if (payload.TryGetProperty("status", out var st) && st.ValueKind == JsonValueKind.String)
        {
            var s = st.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(s) &&
                (s.Equals("success", StringComparison.OrdinalIgnoreCase) ||
                 s.Equals("deleted", StringComparison.OrdinalIgnoreCase) ||
                 s.Equals("ok", StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        // If a lifecycle reply has no error and does not indicate failure, treat as success.
        if (payload.TryGetProperty("error", out var err) &&
            err.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
            return false;

        if (payload.TryGetProperty("success", out var suc2) && suc2.ValueKind == JsonValueKind.False)
            return false;

        return true;
    }

    private static (string? Message, string? Code) ReadErrorFromPayload(JsonElement payload)
    {
        string? message = null;
        string? code = null;

        if (!payload.TryGetProperty("error", out var errEl) &&
            payload.TryGetProperty("errorMessage", out var legacyErr))
        {
            errEl = legacyErr;
        }

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

        // Some workers may send top-level string fields.
        message ??= ReadOptionalString(payload, "errorText") ?? ReadOptionalString(payload, "message");
        code ??= ReadJsonScalarAsTrimmedString(payload, "code");

        return (message, code);
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

        // Workers sometimes put CRM template PK only in payload.id (GUID string); Meta numeric ids won't parse as Guid.
        if (payload.ValueKind == JsonValueKind.Object &&
            payload.TryGetProperty("id", out var payloadIdEl))
        {
            var idRaw = JsonStringOrRaw(payloadIdEl).Trim();
            if (!string.IsNullOrWhiteSpace(idRaw) && Guid.TryParse(idRaw, out var payloadPk))
            {
                var byPayloadId = await db.WhatsAppTemplates
                    .FirstOrDefaultAsync(t => t.Id == payloadPk, cancellationToken);
                if (byPayloadId != null)
                    return byPayloadId;
            }
        }

        // CRM WABA row id (from API) + template name — reliable when identifications use correct keys but Meta WABA id mismatches DB.
        if (identifications.ValueKind == JsonValueKind.Object &&
            TryGetPropertyIgnoreCase(identifications, "whatsAppBusinessAccountId", out var crmWabaTplEl))
        {
            var crmWabaStr = JsonStringOrRaw(crmWabaTplEl).Trim();
            if (!string.IsNullOrWhiteSpace(crmWabaStr) && Guid.TryParse(crmWabaStr, out var crmWabaPk) &&
                payload.ValueKind == JsonValueKind.Object &&
                payload.TryGetProperty("name", out var nameByWabaEl))
            {
                var nameByWaba = JsonStringOrRaw(nameByWabaEl);
                if (!string.IsNullOrWhiteSpace(nameByWaba))
                {
                    var qWaba = db.WhatsAppTemplates.Where(t =>
                        t.WhatsAppBusinessAccountId == crmWabaPk && t.Name == nameByWaba.Trim());
                    if ((payload.TryGetProperty("language", out var langWabaEl) ||
                         payload.TryGetProperty("languague", out langWabaEl)) && langWabaEl.ValueKind == JsonValueKind.String)
                    {
                        var langWaba = langWabaEl.GetString();
                        if (!string.IsNullOrWhiteSpace(langWaba))
                            qWaba = qWaba.Where(t => t.Language == langWaba);
                    }

                    var byCrmWaba = await qWaba.FirstOrDefaultAsync(cancellationToken);
                    if (byCrmWaba != null)
                        return byCrmWaba;
                }
            }
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
