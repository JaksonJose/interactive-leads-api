using System.Text.Json;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Common.PhoneNumbers;
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

        await EnforceWhatsApp24hTemplatePolicyAsync(conversation.Id, conversation.Integration.Type, messageType, cancellationToken);

        OutboundTemplateMessageContentContract? outboundTemplateContent = null;
        WhatsAppTemplate? templateRow = null;
        if (messageType == MessageType.Template)
        {
            if (!request.TemplateId.HasValue)
            {
                var response = new ResultResponse();
                response.AddErrorMessage("TemplateId is required for template messages.", "chat.message.template_id_required");
                throw new BadRequestException(response);
            }

            var wabaId = conversation.Integration.WhatsAppBusinessAccountId;
            if (!wabaId.HasValue)
            {
                var response = new ResultResponse();
                response.AddErrorMessage(
                    "WhatsApp Business Account (WABA) is not set for this integration; templates are unavailable.",
                    "chat.message.waba_missing_for_template");
                throw new BadRequestException(response);
            }

            templateRow = await db.WhatsAppTemplates
                .AsNoTracking()
                .SingleOrDefaultAsync(t => t.Id == request.TemplateId.Value, cancellationToken);

            if (templateRow is null || templateRow.WhatsAppBusinessAccountId != wabaId.Value)
            {
                var response = new ResultResponse();
                response.AddErrorMessage(
                    "Template does not belong to this WhatsApp Business Account (WABA).",
                    "chat.message.template_not_available");
                throw new BadRequestException(response);
            }

            outboundTemplateContent = BuildOutboundTemplateContent(templateRow, request);
        }

        var normalizedPhone = PhoneNumberNormalizer.ToNormalizedDigits(conversation.Contact.Phone ?? string.Empty, "BR");
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
        if (messageType == MessageType.Template && templateRow is not null)
        {
            var friendly = $"[Template] {templateRow.Name}";
            if (!string.IsNullOrWhiteSpace(request.Content))
                friendly = request.Content.Trim();
            messageContent = friendly;
        }

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
            message.Id,
            idempotencyMessageId,
            messageType,
            request.Content?.Trim() ?? string.Empty,
            request.MediaUrl?.Trim(),
            request.Caption?.Trim(),
            request.FileName?.Trim(),
            request.Voice,
            request.ReactionEmoji?.Trim(),
            request.ReactionMessageId,
            request.ReplyToMessageId,
            whatsappSettings,
            outboundTemplateContent);

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
        var rawUrl = request.MediaUrl?.Trim();
        if (string.IsNullOrWhiteSpace(rawUrl))
            return;

        var mediaType = ToMessageMediaType(messageType);
        if (mediaType is null)
            return;

        var persistedUrl = rawUrl;
        var persistedMime = (request.MimeType ?? string.Empty).Trim();
        if (messageType == MessageType.Image)
        {
            var optimized = request.MediaOptimizedUrl?.Trim();
            if (!string.IsNullOrWhiteSpace(optimized))
            {
                persistedUrl = optimized;
                persistedMime = "image/webp";
            }
        }
        else if (messageType == MessageType.Audio)
        {
            var optimized = request.MediaOptimizedUrl?.Trim();
            if (!string.IsNullOrWhiteSpace(optimized))
            {
                persistedUrl = optimized;
                var om = request.MediaOptimizedMimeType?.Trim();
                persistedMime = string.IsNullOrWhiteSpace(om)
                    ? (request.MimeType ?? string.Empty).Trim()
                    : om;
            }
        }

        db.MessageMedia.Add(new MessageMedia
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            MediaType = mediaType.Value,
            Url = persistedUrl,
            MimeType = persistedMime,
            FileSize = 0,
            FileName = ResolveOutboundAudioFileNameForPersistence(request, messageType),
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
        var raw = request.MediaUrl?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (ToMessageMediaType(messageType) is null)
            return null;

        var optimized = request.MediaOptimizedUrl?.Trim();
        var displayUrl = !string.IsNullOrWhiteSpace(optimized) ? optimized : raw;
        var displayMime = (messageType, string.IsNullOrWhiteSpace(optimized)) switch
        {
            (MessageType.Image, false) => "image/webp",
            (MessageType.Audio, false) => (request.MediaOptimizedMimeType ?? request.MimeType ?? "audio/mp4").Trim(),
            _ => (request.MimeType ?? string.Empty).Trim()
        };

        return new MessageMediaListItemDto
        {
            Url = displayUrl,
            OptimizedUrl = displayUrl,
            ThumbnailUrl = string.IsNullOrWhiteSpace(request.MediaThumbnailUrl)
                ? null
                : request.MediaThumbnailUrl.Trim(),
            MimeType = displayMime,
            FileName = ResolveOutboundAudioFileNameForPersistence(request, messageType),
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
                ContactId = conversation.ContactId,
                ContactName = conversation.Contact?.Name,
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
            MessageType.Template => request.Content?.Trim()
                ?? (!request.TemplateId.HasValue ? "[Template]" : $"[Template] {request.TemplateId.Value:D}"),
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
                mediaOptimizedUrl = string.IsNullOrWhiteSpace(request.MediaOptimizedUrl)
                    ? null
                    : request.MediaOptimizedUrl.Trim(),
                mediaThumbnailUrl = string.IsNullOrWhiteSpace(request.MediaThumbnailUrl)
                    ? null
                    : request.MediaThumbnailUrl.Trim(),
                caption = request.Caption?.Trim(),
                fileName = request.FileName?.Trim(),
                mimeType = request.MimeType?.Trim(),
                voice = request.Voice,
                reactionEmoji = request.ReactionEmoji?.Trim(),
                reactionMessageId = request.ReactionMessageId,
                replyToMessageId = request.ReplyToMessageId,
                templateId = request.TemplateId,
                templateBodyParameters = request.TemplateBodyParameters,
                templateHeaderParameter = request.TemplateHeaderParameter
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

    private async Task EnforceWhatsApp24hTemplatePolicyAsync(
        Guid conversationId,
        IntegrationType integrationType,
        MessageType messageType,
        CancellationToken cancellationToken)
    {
        if (integrationType != IntegrationType.WhatsApp)
            return;

        if (messageType == MessageType.Template)
            return;

        var lastInboundAt = await db.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId && m.Direction == MessageDirection.Inbound)
            .OrderByDescending(m => m.MessageDate)
            .Select(m => (DateTimeOffset?)m.MessageDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (!lastInboundAt.HasValue)
        {
            var response = new ResultResponse();
            response.AddErrorMessage(
                "WhatsApp 24h window expired or missing; template is required to re-open the conversation.",
                "chat.message.whatsapp_template_required");
            throw new BadRequestException(response);
        }

        var freeReplyUntil = lastInboundAt.Value.AddHours(24);
        if (DateTimeOffset.UtcNow <= freeReplyUntil)
            return;

        var responseExpired = new ResultResponse();
        responseExpired.AddErrorMessage(
            "WhatsApp 24h window expired; template is required to re-open the conversation.",
            "chat.message.whatsapp_template_required");
        throw new BadRequestException(responseExpired);
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
            "template" => MessageType.Template,
            _ => MessageType.Text
        };
    }

    private static OutboundTemplateMessageContentContract BuildOutboundTemplateContent(
        WhatsAppTemplate templateRow,
        SendConversationMessageRequest request)
    {
        var components = new List<OutboundTemplateComponentContract>();

        var headerParam = (request.TemplateHeaderParameter ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(headerParam))
        {
            components.Add(new OutboundTemplateComponentContract(
                Type: "header",
                Parameters: new[]
                {
                    new OutboundTemplateParameterContract(Type: "text", Text: headerParam)
                }));
        }

        var bodyParams = request.TemplateBodyParameters ?? Array.Empty<string>();
        if (bodyParams.Length > 0)
        {
            components.Add(new OutboundTemplateComponentContract(
                Type: "body",
                Parameters: bodyParams
                    .Select(x => new OutboundTemplateParameterContract(Type: "text", Text: (x ?? string.Empty).Trim()))
                    .ToList()));
        }

        var providerTemplateId = string.IsNullOrWhiteSpace(templateRow.MetaTemplateId)
            ? templateRow.Id.ToString("D")
            : templateRow.MetaTemplateId.Trim();

        return new OutboundTemplateMessageContentContract(
            TemplateId: providerTemplateId,
            Name: templateRow.Name,
            Language: templateRow.Language,
            Components: components.Count == 0 ? null : components);
    }

    /// <summary>CRM row: for transcoded audio, store the M4A file name; otherwise the delivery file name.</summary>
    private static string? ResolveOutboundAudioFileNameForPersistence(SendConversationMessageRequest request, MessageType messageType)
    {
        if (messageType == MessageType.Audio)
        {
            var optUrl = request.MediaOptimizedUrl?.Trim();
            var optFn = request.MediaOptimizedFileName?.Trim();
            if (!string.IsNullOrWhiteSpace(optUrl) && !string.IsNullOrWhiteSpace(optFn))
                return optFn;
        }

        return string.IsNullOrWhiteSpace(request.FileName) ? null : request.FileName.Trim();
    }
}
