using System.Text.Json;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Feature.Chat.Messages.Outbound;
using InteractiveLeads.Application.Integrations.Settings;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Realtime.Models;
using InteractiveLeads.Application.Realtime.Services;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InteractiveLeads.Application.Feature.Chat.Messages.Services;

public sealed class MessageService(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IRealtimeService realtimeService,
    IIntegrationSettingsResolver integrationSettingsResolver,
    IN8nClient n8nClient,
    ILogger<MessageService> logger) : IMessageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<MessageListItemDto> SendConversationMessageAsync(
        Guid conversationId,
        SendConversationMessageRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);
        var tenantId = currentUserService.GetUserTenant();

        var conversation = await db.Conversations
            .Include(c => c.Inbox)
            .Include(c => c.Integration)
            .Include(c => c.Contact)
            .Where(c => c.Id == conversationId && c.CompanyId == companyId)
            .SingleOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Conversation not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        await ChatContext.EnsureInboxAccessAsync(db, currentUserService, conversation.InboxId, companyId, cancellationToken);

        var messageType = ParseMessageType(request.Type);
        var idempotencyMessageId = string.IsNullOrWhiteSpace(request.ExternalMessageId)
            ? $"msg_{Guid.NewGuid():N}"
            : request.ExternalMessageId.Trim();

        var existingMessage = await db.Messages
            .AsNoTracking()
            .Where(m => m.ExternalMessageId == idempotencyMessageId)
            .Select(m => new { m.Id, m.Content, m.CreatedAt, m.Direction })
            .SingleOrDefaultAsync(cancellationToken);

        if (existingMessage is not null)
        {
            logger.LogInformation(
                "Outbound message deduplicated for conversation {ConversationId} with external message id {ExternalMessageId}",
                conversation.Id,
                idempotencyMessageId);

            return new MessageListItemDto
            {
                Id = existingMessage.Id,
                Content = existingMessage.Content,
                Direction = existingMessage.Direction == MessageDirection.Inbound ? "inbound" : "outbound",
                CreatedAt = existingMessage.CreatedAt
            };
        }

        ValidateProviderSupport(conversation);
        await EnsureReplyAndReactionMessageAsync(conversation.Id, request, messageType, cancellationToken);

        var normalizedPhone = NormalizePhoneNumber(conversation.Contact.Phone ?? string.Empty);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Contact phone is required for outbound external delivery.", "chat.message.phone_required");
            throw new BadRequestException(response);
        }

        var now = DateTimeOffset.UtcNow;
        var senderUserId = Guid.TryParse(currentUserService.GetUserId(), out var userGuid) ? userGuid : (Guid?)null;
        var metadataJson = BuildMessageMetadataJson(conversation, request, idempotencyMessageId);
        var messageContent = ResolvePersistedMessageContent(request, messageType);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            ExternalMessageId = idempotencyMessageId,
            Direction = MessageDirection.Outbound,
            Type = messageType,
            Content = messageContent,
            ReplyToMessageId = request.ReplyToMessageId,
            Status = MessageStatus.Pending,
            Metadata = metadataJson,
            SenderUserId = senderUserId,
            CreatedAt = now
        };

        db.Messages.Add(message);
        conversation.Status = ConversationStatus.Open;
        if (conversation.LastMessageAt < now)
            conversation.LastMessageAt = now;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Outbound message persisted with pending status for conversation {ConversationId}, message {MessageId}, type {MessageType}",
            conversation.Id,
            message.Id,
            message.Type);

        var whatsappSettings = TryGetWhatsAppSettings(conversation);
        var payload = OutboundMessageContractMapper.Create(
            tenantId,
            conversation,
            normalizedPhone,
            idempotencyMessageId,
            messageType,
            request.Content?.Trim() ?? string.Empty,
            request.MediaUrl?.Trim(),
            request.Caption?.Trim(),
            request.ReactionEmoji?.Trim(),
            request.ReactionMessageId,
            request.ReplyToMessageId,
            whatsappSettings);

        var dispatchResult = await n8nClient.SendMessageAsync(payload, cancellationToken);
        if (dispatchResult.HasAnyErrorMessage)
        {
            message.Status = MessageStatus.Failed;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogError(
                "Outbound dispatch failed for conversation {ConversationId}, message {MessageId}, externalId {ExternalMessageId}",
                conversation.Id,
                message.Id,
                idempotencyMessageId);

            var response = new ResultResponse();
            response.AddErrorMessage("External provider rejected message delivery.", "chat.message.external_send_failed");
            foreach (var item in dispatchResult.Messages)
                response.Messages.Add(item);
            throw new BadRequestException(response);
        }

        message.Status = MessageStatus.Sent;
        await db.SaveChangesAsync(cancellationToken);

        await PublishRealtimeEventsAsync(conversation, message, tenantId);

        logger.LogInformation(
            "Outbound message sent successfully for conversation {ConversationId}, message {MessageId}, externalId {ExternalMessageId}",
            conversation.Id,
            message.Id,
            idempotencyMessageId);

        return new MessageListItemDto
        {
            Id = message.Id,
            Content = message.Content,
            Direction = "outbound",
            CreatedAt = message.CreatedAt
        };
    }

    private async Task PublishRealtimeEventsAsync(Conversation conversation, Message message, string tenantId)
    {
        var messageCreatedEvent = new RealtimeEvent<MessageCreatedPayloadDto>
        {
            Type = "message.created",
            TenantId = tenantId,
            Timestamp = DateTime.UtcNow,
            Payload = new MessageCreatedPayloadDto
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                Text = message.Content,
                SenderId = message.SenderUserId?.ToString(),
                CreatedAt = message.CreatedAt
            }
        };

        await realtimeService.SendToConversationAsync(conversation.Id.ToString(), messageCreatedEvent);

        var movedEvent = new RealtimeEvent<ConversationMovedToTopPayloadDto>
        {
            Type = "conversation.moved_to_top",
            TenantId = tenantId,
            Timestamp = DateTime.UtcNow,
            Payload = new ConversationMovedToTopPayloadDto
            {
                Id = conversation.Id,
                LastMessage = message.Content,
                LastMessageAt = conversation.LastMessageAt
            }
        };

        await realtimeService.SendToInboxAsync(conversation.InboxId.ToString(), movedEvent);
    }

    private async Task EnsureReplyAndReactionMessageAsync(
        Guid conversationId,
        SendConversationMessageRequest request,
        MessageType messageType,
        CancellationToken cancellationToken)
    {
        if (messageType == MessageType.Reaction)
        {
            var targetMessageId = request.ReactionMessageId ?? request.ReplyToMessageId;
            if (!targetMessageId.HasValue)
                return;

            var exists = await db.Messages
                .AsNoTracking()
                .AnyAsync(m => m.Id == targetMessageId.Value && m.ConversationId == conversationId, cancellationToken);
            if (!exists)
            {
                var response = new ResultResponse();
                response.AddErrorMessage("Reaction target message not found.", "chat.message.reaction_target_not_found");
                throw new BadRequestException(response);
            }

            request.ReplyToMessageId = targetMessageId;
            return;
        }

        if (!request.ReplyToMessageId.HasValue)
            return;

        var replyExists = await db.Messages
            .AsNoTracking()
            .AnyAsync(m => m.Id == request.ReplyToMessageId.Value && m.ConversationId == conversationId, cancellationToken);
        if (!replyExists)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Reply target message not found.", "chat.message.reply_target_not_found");
            throw new BadRequestException(response);
        }
    }

    private static string ResolvePersistedMessageContent(SendConversationMessageRequest request, MessageType messageType)
    {
        return messageType switch
        {
            MessageType.Image => request.Caption?.Trim() ?? request.MediaUrl?.Trim() ?? string.Empty,
            MessageType.Video => request.Caption?.Trim() ?? request.MediaUrl?.Trim() ?? string.Empty,
            MessageType.Reaction => request.ReactionEmoji?.Trim() ?? string.Empty,
            _ => request.Content?.Trim() ?? string.Empty
        };
    }

    private static string BuildMessageMetadataJson(Conversation conversation, SendConversationMessageRequest request, string messageId)
    {
        var metadata = new
        {
            integrationType = conversation.Integration.Type.ToString(),
            outbound = new
            {
                messageId,
                messageType = request.Type?.Trim().ToLowerInvariant(),
                mediaUrl = request.MediaUrl?.Trim(),
                caption = request.Caption?.Trim(),
                reactionEmoji = request.ReactionEmoji?.Trim(),
                reactionMessageId = request.ReactionMessageId,
                replyToMessageId = request.ReplyToMessageId
            }
        };

        return JsonSerializer.Serialize(metadata, JsonOptions);
    }

    private WhatsAppSettings? TryGetWhatsAppSettings(Conversation conversation)
    {
        if (conversation.Integration.Type != IntegrationType.WhatsApp)
            return null;

        var settings = integrationSettingsResolver.Deserialize(conversation.Integration.Type, conversation.Integration.Settings);
        return settings as WhatsAppSettings;
    }

    private static void ValidateProviderSupport(Conversation conversation)
    {
        if (conversation.Integration.Type == IntegrationType.WhatsApp)
            return;

        var response = new ResultResponse();
        response.AddErrorMessage("Integration type not implemented for outbound messages yet.", "chat.message.integration_not_supported");
        throw new BadRequestException(response);
    }

    private static MessageType ParseMessageType(string? type)
    {
        var normalized = type?.Trim().ToLowerInvariant() ?? "text";
        return normalized switch
        {
            "text" => MessageType.Text,
            "image" => MessageType.Image,
            "video" => MessageType.Video,
            "reaction" => MessageType.Reaction,
            "reply" => MessageType.Reply,
            _ => MessageType.Text
        };
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits))
            return string.Empty;

        if (digits.StartsWith("00", StringComparison.Ordinal))
            digits = digits[2..];

        return digits;
    }
}
