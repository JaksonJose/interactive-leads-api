using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace InteractiveLeads.Application.Feature.Chat.Messages;

/// <summary>
/// <see cref="Domain.Entities.Message.Metadata"/> stores multiple concerns (outbound envelope, UI snapshots, media pipeline, provider webhooks).
/// Patches must merge into the existing JSON instead of replacing the document so prior keys are not lost.
/// </summary>
public static class MessageMetadataMerge
{
    internal static readonly JsonSerializerOptions DefaultWriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static JsonObject ParseOrEmpty(string? existingMetadata)
    {
        if (string.IsNullOrWhiteSpace(existingMetadata))
            return new JsonObject();
        try
        {
            var node = JsonNode.Parse(existingMetadata);
            return node as JsonObject ?? new JsonObject { ["_legacyRoot"] = node };
        }
        catch
        {
            return new JsonObject();
        }
    }

    /// <summary>Append/replace the latest raw provider payload (status, ack, etc.) without dropping <c>templateSnapshot</c> or <c>outbound</c>.</summary>
    public static string WithLastInboundProviderEvent(
        string? existingMetadata,
        object eventPayload,
        JsonSerializerOptions serializerOptions)
    {
        var root = ParseOrEmpty(existingMetadata);
        root["lastInboundProviderEvent"] = JsonSerializer.SerializeToNode(eventPayload, serializerOptions);
        return root.ToJsonString(serializerOptions);
    }

    public static string WithMediaProcessingComplete(
        string? existingMetadata,
        object mediaProcessingPayload,
        JsonSerializerOptions? serializerOptions = null)
    {
        var opts = serializerOptions ?? DefaultWriteOptions;
        var root = ParseOrEmpty(existingMetadata);
        root["mediaProcessingStatus"] = "completed";
        root["mediaProcessing"] = JsonSerializer.SerializeToNode(mediaProcessingPayload, opts);
        root.Remove("error");
        return root.ToJsonString(opts);
    }

    public static string WithMediaProcessingFailed(
        string? existingMetadata,
        string errorMessage,
        JsonSerializerOptions? serializerOptions = null)
    {
        var opts = serializerOptions ?? DefaultWriteOptions;
        var root = ParseOrEmpty(existingMetadata);
        root["mediaProcessingStatus"] = "failed";
        root["error"] = errorMessage;
        return root.ToJsonString(opts);
    }
}
