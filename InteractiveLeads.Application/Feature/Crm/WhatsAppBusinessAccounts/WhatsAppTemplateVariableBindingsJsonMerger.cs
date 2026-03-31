using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using InteractiveLeads.Application.Responses;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

internal static class WhatsAppTemplateVariableBindingsJsonMerger
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions WriteOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions NodeOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Merges <paramref name="bindings"/> into <c>ComponentsJson</c> without calling Meta.</summary>
    public static ResultResponse? TryMerge(string componentsJson, WhatsAppTemplateVariableBindingDto[] bindings, out string newJson)
    {
        newJson = componentsJson;
        if (string.IsNullOrWhiteSpace(componentsJson) || componentsJson.Trim() == "{}")
        {
            var err = new ResultResponse();
            err.AddErrorMessage("Template components are missing.", "integrations.templates.components_missing");
            return err;
        }

        try
        {
            using var doc = JsonDocument.Parse(componentsJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("source", out var srcEl)
                && string.Equals(srcEl.GetString(), "meta_template_sync", StringComparison.OrdinalIgnoreCase))
            {
                return MergeViaJsonObject(componentsJson, bindings, out newJson);
            }

            if (LooksLikePersistedSchema(root))
            {
                var persisted = JsonSerializer.Deserialize<WhatsAppTemplatePersistedComponents>(componentsJson, JsonOpts);
                if (persisted == null)
                {
                    var err = new ResultResponse();
                    err.AddErrorMessage("Could not read template components.", "integrations.templates.components_invalid");
                    return err;
                }

                persisted.VariableBindings = bindings.Length > 0 ? bindings : null;
                if (persisted.SchemaVersion < 1)
                    persisted.SchemaVersion = 1;

                newJson = JsonSerializer.Serialize(persisted, WriteOpts);
                return null;
            }

            return MergeViaJsonObject(componentsJson, bindings, out newJson);
        }
        catch (JsonException)
        {
            var err = new ResultResponse();
            err.AddErrorMessage("Could not read template components.", "integrations.templates.components_invalid");
            return err;
        }
    }

    private static bool LooksLikePersistedSchema(JsonElement root) =>
        (root.TryGetProperty("schemaVersion", out var sv) && sv.ValueKind == JsonValueKind.Number)
        || (root.TryGetProperty("variableBindings", out var vb) && vb.ValueKind == JsonValueKind.Array
            && root.TryGetProperty("body", out var b) && b.ValueKind == JsonValueKind.String);

    private static ResultResponse? MergeViaJsonObject(string componentsJson, WhatsAppTemplateVariableBindingDto[] bindings, out string newJson)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(componentsJson);
        }
        catch (JsonException)
        {
            newJson = componentsJson;
            var err = new ResultResponse();
            err.AddErrorMessage("Could not read template components.", "integrations.templates.components_invalid");
            return err;
        }

        if (node is not JsonObject obj)
        {
            newJson = componentsJson;
            var err = new ResultResponse();
            err.AddErrorMessage("Template components must be a JSON object.", "integrations.templates.components_invalid");
            return err;
        }

        if (bindings.Length == 0)
            obj.Remove("variableBindings");
        else
            obj["variableBindings"] = JsonSerializer.SerializeToNode(bindings, NodeOpts);

        newJson = obj.ToJsonString(WriteOpts);
        return null;
    }
}
