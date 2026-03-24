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
    IOutboundMessageDispatcher outboundDispatcher,
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
            .Select(m => new
            {
                m.Id,
                m.Content,
                Type = m.Type.ToString().ToLowerInvariant(),
                Media = m.Media
                    .OrderBy(x => x.Id)
                    .Select(x => new MessageMediaListItemDto
                    {
                        Url = x.Url,
                        MimeType = x.MimeType,
                        FileName = x.FileName,
                        Animated = x.Animated,
                        Voice = x.Voice,
                        Caption = x.Caption
                    })
                    .FirstOrDefault(),
                m.MessageDate,
                m.CreatedAt,
                m.UpdatedAt,
                m.Direction,
                m.Status
            })
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
                Type = existingMessage.Type,
                Media = existingMessage.Media,
                Direction = existingMessage.Direction == MessageDirection.Inbound ? "inbound" : "outbound",
                MessageDate = existingMessage.MessageDate,
                CreatedAt = existingMessage.CreatedAt,
                UpdatedAt = existingMessage.UpdatedAt,
                Status = MessageListItemDtoMapper.ToStatusString(existingMessage.Status)
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

        var persistNow = DateTimeOffset.UtcNow;
        var messageDate = request.ClientTimestamp is { } ts && ts > 0
            ? DateTimeOffset.FromUnixTimeSeconds(ts)
            : persistNow;
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
            MessageDate = messageDate,
            CreatedAt = persistNow,
            UpdatedAt = persistNow
        };

        db.Messages.Add(message);
        AddOutboundMessageMedia(message.Id, request, messageType);
        conversation.Status = ConversationStatus.Open;
        conversation.LastMessage = message.Content;
        if (conversation.LastMessageAt < messageDate)
            conversation.LastMessageAt = messageDate;
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
            message.Id,
            messageType,
            request.Content?.Trim() ?? string.Empty,
            request.MediaUrl?.Trim(),
            request.Caption?.Trim(),
            request.FileName?.Trim(),
            request.MimeType?.Trim(),
            request.Voice,
            request.ReactionEmoji?.Trim(),
            request.ReactionMessageId,
            request.ReplyToMessageId,
            whatsappSettings);

        var dispatchOutcome = await outboundDispatcher.SendMessageAsync(payload, cancellationToken);
        if (dispatchOutcome.Response.HasAnyErrorMessage)
        {
            message.Status = MessageStatus.Failed;
            message.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogError(
                "Outbound dispatch failed for conversation {ConversationId}, message {MessageId}, externalId {ExternalMessageId}",
                conversation.Id,
                message.Id,
                idempotencyMessageId);

            var response = new ResultResponse();
            response.AddErrorMessage("External provider rejected message delivery.", "chat.message.external_send_failed");
            foreach (var item in dispatchOutcome.Response.Messages)
                response.Messages.Add(item);
            throw new BadRequestException(response);
        }

        if (dispatchOutcome.AdvanceToSentOnSuccess)
        {
            message.Status = MessageStatus.Sent;
            message.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        var outboundMediaDto = BuildOutboundMediaListItem(request, messageType);
        await PublishRealtimeEventsAsync(conversation, message, tenantId, outboundMediaDto);

        logger.LogInformation(
            dispatchOutcome.AdvanceToSentOnSuccess
                ? "Outbound message dispatched and marked sent for conversation {ConversationId}, message {MessageId}, externalId {ExternalMessageId}"
                : "Outbound message queued (pending until provider ack) for conversation {ConversationId}, message {MessageId}, externalId {ExternalMessageId}",
            conversation.Id,
            message.Id,
            idempotencyMessageId);

        return new MessageListItemDto
        {
            Id = message.Id,
            Content = message.Content,
            Type = message.Type.ToString().ToLowerInvariant(),
            Media = outboundMediaDto,
            Direction = "outbound",
            MessageDate = message.MessageDate,
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt,
            Status = MessageListItemDtoMapper.ToStatusString(message.Status),
            MediaProcessingStatus = "completed"
        };
    }

    private void AddOutboundMessageMedia(
        Guid messageId,
        SendConversationMessageRequest request,
        MessageType messageType)
    {
        var url = request.MediaUrl?.Trim();
        if (string.IsNullOrWhiteSpace(url))
            return;

        var mediaType = ToMessageMediaType(messageType);
        if (mediaType is null)
            return;

        db.MessageMedia.Add(new MessageMedia
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            MediaType = mediaType.Value,
            Url = url,
            MimeType = (request.MimeType ?? string.Empty).Trim(),
            FileSize = 0,
            FileName = string.IsNullOrWhiteSpace(request.FileName) ? null : request.FileName.Trim(),
            Animated = false,
            Voice = request.Voice ?? false,
            Caption = string.IsNullOrWhiteSpace(request.Caption) ? null : request.Caption.Trim()
        });
    }

    private static MediaType? ToMessageMediaType(MessageType messageType) =>
        messageType switch
        {
            MessageType.Image => MediaType.Image,
            MessageType.Video => MediaType.Video,
            MessageType.Audio => MediaType.Audio,
            MessageType.Document => MediaType.Document,
            _ => null
        };

    private static MessageMediaListItemDto? BuildOutboundMediaListItem(
        SendConversationMessageRequest request,
        MessageType messageType)
    {
        var url = request.MediaUrl?.Trim();
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (ToMessageMediaType(messageType) is null)
            return null;

        return new MessageMediaListItemDto
        {
            Url = url,
            OptimizedUrl = url,
            ThumbnailUrl = string.IsNullOrWhiteSpace(request.MediaThumbnailUrl)
                ? null
                : request.MediaThumbnailUrl.Trim(),
            MimeType = (request.MimeType ?? string.Empty).Trim(),
            FileName = string.IsNullOrWhiteSpace(request.FileName) ? null : request.FileName.Trim(),
            Voice = request.Voice ?? false,
            Caption = string.IsNullOrWhiteSpace(request.Caption) ? null : request.Caption.Trim()
        };
    }

    private async Task PublishRealtimeEventsAsync(
        Conversation conversation,
        Message message,
        string tenantId,
        MessageMediaListItemDto? media)
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
                Type = message.Type.ToString().ToLowerInvariant(),
                Media = media,
                SenderId = message.SenderUserId?.ToString(),
                ExternalMessageId = message.ExternalMessageId,
                MessageDate = message.MessageDate,
                CreatedAt = message.MessageDate,
                Status = MessageListItemDtoMapper.ToStatusString(message.Status),
                MediaProcessingStatus = "completed"
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
            MessageType.Image => request.Caption?.Trim()
                ?? request.FileName?.Trim()
                ?? string.Empty,
            MessageType.Video => request.Caption?.Trim()
                ?? request.FileName?.Trim()
                ?? string.Empty,
            MessageType.Document => request.Caption?.Trim()
                ?? request.FileName?.Trim()
                ?? request.MediaUrl?.Trim()
                ?? string.Empty,
            MessageType.Audio => request.Caption?.Trim()
                ?? request.FileName?.Trim()
                ?? string.Empty,
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
                mediaOriginalUrl = string.IsNullOrWhiteSpace(request.MediaOriginalUrl)
                    ? null
                    : request.MediaOriginalUrl.Trim(),
                mediaThumbnailUrl = string.IsNullOrWhiteSpace(request.MediaThumbnailUrl)
                    ? null
                    : request.MediaThumbnailUrl.Trim(),
                caption = request.Caption?.Trim(),
                fileName = request.FileName?.Trim(),
                mimeType = request.MimeType?.Trim(),
                voice = request.Voice,
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
            "audio" => MessageType.Audio,
            "document" => MessageType.Document,
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
