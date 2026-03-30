using System.Text.Json.Serialization;

namespace InteractiveLeads.Application.Feature.Chat.Messages.Outbound;

public sealed record OutboundMessageContract(
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("tenantId")] string TenantId,
    [property: JsonPropertyName("channelId")] string ChannelId,
    [property: JsonPropertyName("auth")] OutboundAuthContract? Auth,
    [property: JsonPropertyName("contact")] OutboundContactContract Contact,
    [property: JsonPropertyName("payload")] OutboundMessageBodyContract Payload,
    [property: JsonPropertyName("metadata")] OutboundMetadataContract Metadata);

/// <summary>WhatsApp Cloud API credentials sent with outbound messages.</summary>
public sealed record OutboundAuthContract(
    [property: JsonPropertyName("accessToken")] string AccessToken,
    [property: JsonPropertyName("phoneNumberId")] string PhoneNumberId,
    [property: JsonPropertyName("businessAccountId")] string BusinessAccountId);

public sealed record OutboundContactContract(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("phoneNumber")] string PhoneNumber);

public sealed record OutboundMessageBodyContract(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("clientMessageId")] string ClientMessageId,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("content")] object Content);

public sealed record OutboundMetadataContract(
    [property: JsonPropertyName("conversationId")] string ConversationId,
    [property: JsonPropertyName("replyToMessageId")] string? ReplyToMessageId);

public sealed record OutboundTextContentContract(
    [property: JsonPropertyName("body")] string Body);

public sealed record OutboundImageContentContract(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("caption")] string? Caption);

public sealed record OutboundVideoContentContract(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("caption")] string? Caption);

public sealed record OutboundReactionContentContract(
    [property: JsonPropertyName("emoji")] string Emoji,
    [property: JsonPropertyName("messageId")] string MessageId);

public sealed record OutboundReplyContentContract(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("messageId")] string MessageId);

public sealed record OutboundDocumentContentContract(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("caption")] string? Caption,
    [property: JsonPropertyName("fileName")] string FileName);

public sealed record OutboundAudioContentContract(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("voice")] bool Voice);

/// <summary>
/// WhatsApp Cloud API template message payload.
/// Shape is intentionally close to Meta \"messages\" endpoint template format.
/// </summary>
public sealed record OutboundTemplateMessageContentContract(
    [property: JsonPropertyName("templateId")] string TemplateId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("components")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<OutboundTemplateComponentContract>? Components);

public sealed record OutboundTemplateComponentContract(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("parameters")] IReadOnlyList<OutboundTemplateParameterContract> Parameters);

public sealed record OutboundTemplateParameterContract(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string Text);
