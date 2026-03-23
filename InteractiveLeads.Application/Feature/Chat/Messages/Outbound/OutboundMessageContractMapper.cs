using InteractiveLeads.Application.Integrations.Settings;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Feature.Chat.Messages.Outbound;

internal static class OutboundMessageContractMapper
{
    public static OutboundMessageContract Create(
        string tenantId,
        Conversation conversation,
        string normalizedPhoneNumber,
        string messageId,
        Guid clientMessageId,
        MessageType messageType,
        string content,
        string? mediaUrl,
        string? caption,
        string? reactionEmoji,
        Guid? reactionMessageId,
        Guid? replyToMessageId,
        WhatsAppSettings? authSettings)
    {
        var messageTypeName = ToContractType(messageType);
        var payload = new OutboundMessageContract(
            Provider: ToProvider(conversation.Integration.Type),
            EventType: "send_message",
            TenantId: tenantId,
            ChannelId: conversation.Integration.ExternalIdentifier,
            Auth: BuildAuth(authSettings),
            Contact: new OutboundContactContract(
                Name: conversation.Contact.Name,
                PhoneNumber: normalizedPhoneNumber),
            Payload: new OutboundMessageBodyContract(
                Id: messageId,
                ClientMessageId: clientMessageId.ToString("D"),
                Type: messageTypeName,
                Content: BuildContent(messageType, content, mediaUrl, caption, reactionEmoji, reactionMessageId, replyToMessageId)),
            Metadata: new OutboundMetadataContract(
                ConversationId: conversation.Id.ToString(),
                ReplyToMessageId: replyToMessageId?.ToString()));

        return payload;
    }

    private static OutboundAuthContract? BuildAuth(WhatsAppSettings? settings)
    {
        if (settings is null)
            return null;

        return new OutboundAuthContract(
            WebhookVerifyToken: WhatsAppWebhookDefaults.FixedVerifyToken,
            PhoneNumberId: settings.PhoneNumberId,
            BusinessAccountId: settings.BusinessAccountId);
    }

    private static object BuildContent(
        MessageType messageType,
        string content,
        string? mediaUrl,
        string? caption,
        string? reactionEmoji,
        Guid? reactionMessageId,
        Guid? replyToMessageId)
    {
        return messageType switch
        {
            MessageType.Image => new OutboundImageContentContract(mediaUrl ?? string.Empty, caption),
            MessageType.Video => new OutboundVideoContentContract(mediaUrl ?? string.Empty, caption),
            MessageType.Reaction => new OutboundReactionContentContract(reactionEmoji ?? string.Empty, reactionMessageId?.ToString() ?? string.Empty),
            MessageType.Reply => new OutboundReplyContentContract(content, replyToMessageId?.ToString() ?? string.Empty),
            _ => new OutboundTextContentContract(content)
        };
    }

    private static string ToContractType(MessageType messageType)
    {
        return messageType switch
        {
            MessageType.Text => "text",
            MessageType.Image => "image",
            MessageType.Video => "video",
            MessageType.Audio => "audio",
            MessageType.Document => "document",
            MessageType.Reaction => "reaction",
            MessageType.Template => "template",
            MessageType.Reply => "reply",
            _ => "text"
        };
    }

    private static string ToProvider(IntegrationType integrationType)
    {
        return integrationType switch
        {
            IntegrationType.WhatsApp => "whatsapp",
            IntegrationType.Instagram => "instagram",
            _ => integrationType.ToString().ToLowerInvariant()
        };
    }
}
