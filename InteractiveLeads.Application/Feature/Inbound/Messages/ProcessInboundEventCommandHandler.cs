using System.Text.Json;
using System.Text.Json.Serialization;
using InteractiveLeads.Application.Feature.Chat.Messages;
using InteractiveLeads.Application.Feature.Inbound;
using InteractiveLeads.Application.Feature.Inbound.Media;
using InteractiveLeads.Application.Integrations.Settings;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Messaging.Contracts;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Realtime.Models;
using InteractiveLeads.Application.Realtime.Services;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Domain.Enums;
using InteractiveLeads.Application.Common.PhoneNumbers;
using InteractiveLeads.Application.Dispatching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InteractiveLeads.Application.Feature.Inbound.Messages;

public sealed class ProcessInboundEventCommandHandler(
    IIntegrationExternalIdentifierLookupRepository integrationLookupRepository,
    ICrossTenantService crossTenantService,
    IRealtimeService realtimeService,
    IMediaProcessingJobPublisher mediaProcessingJobPublisher,
    ILogger<ProcessInboundEventCommandHandler> logger)
    : IApplicationRequestHandler<ProcessInboundEventCommand, IResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public async Task<IResponse> Handle(ProcessInboundEventCommand request, CancellationToken cancellationToken)
    {
        var result = new SingleResponse<InboundProcessingResultDto>(new InboundProcessingResultDto
        {
            Processed = false,
            Outcome = InboundProcessingOutcome.Unknown
        });

        var provider = request.Event.Provider?.Trim();
        var eventType = request.Event.EventType?.Trim();
        var externalIdentifier = request.Event.Identifications?.ExternalIdentifier?.Trim();

        if (string.IsNullOrWhiteSpace(provider) ||
            string.IsNullOrWhiteSpace(eventType) ||
            string.IsNullOrWhiteSpace(externalIdentifier))
        {
            SetPermanent(result, "invalid_payload");
            return result;
        }

        if (!TryMapProvider(provider, out var integrationType))
        {
            logger.LogWarning("Inbound event ignored: unsupported provider {Provider}", provider);
            SetPermanent(result, "unsupported_provider");
            return result;
        }

        if (eventType.Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            await HandleStatusEventAsync(
                request,
                result,
                provider,
                integrationType,
                externalIdentifier,
                cancellationToken);
            return result;
        }

        if (!eventType.Equals("message", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Inbound event received but not implemented for eventType {EventType}", eventType);
            SetPermanent(result, "event_type_not_implemented");
            return result;
        }

        InboundMessagePayload? messagePayload;

        try
        {
            messagePayload = request.Event.Payload.Deserialize<InboundMessagePayload>(JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Inbound event ignored: failed to deserialize message payload");
            SetPermanent(result, "invalid_message_payload");
            return result;
        }

        if (messagePayload == null || string.IsNullOrWhiteSpace(messagePayload.Id))
        {
            SetPermanent(result, "missing_message_id");
            return result;
        }

        var lookup = await integrationLookupRepository.GetByProviderAndExternalIdentifierAsync(
            integrationType,
            externalIdentifier,
            cancellationToken);

        if (lookup == null)
        {
            logger.LogWarning(
                "Inbound event: integration lookup not found for provider {Provider} externalIdentifier {ExternalIdentifier} messageId {MessageId}",
                provider,
                externalIdentifier,
                messagePayload.Id);

            SetPermanent(result, "integration_not_found");
            return result;
        }

        await crossTenantService.ExecuteInTenantContextForSystemAsync(lookup.TenantId, async sp =>
        {
            var db = sp.GetRequiredService<IApplicationDbContext>();
            var settingsResolver = sp.GetRequiredService<IIntegrationSettingsResolver>();
            var tenantLogger = sp.GetRequiredService<ILogger<ProcessInboundEventCommandHandler>>();

            var executionStrategy = db.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

                var integration = await db.Integrations
                    .SingleOrDefaultAsync(i => i.Id == lookup.IntegrationId, cancellationToken);

                if (integration == null)
                {
                    tenantLogger.LogWarning(
                        "Inbound event: integration not found in tenant db. tenantId {TenantId} integrationId {IntegrationId} externalIdentifier {ExternalIdentifier} messageId {MessageId}",
                        lookup.TenantId,
                        lookup.IntegrationId,
                        externalIdentifier,
                        messagePayload.Id);

                    SetPermanent(result, "integration_missing_in_tenant");
                    return;
                }

                var companyId = integration.CompanyId;

                var ackClientMessageId = string.IsNullOrWhiteSpace(messagePayload.ClientMessageId)
                    ? null
                    : messagePayload.ClientMessageId.Trim();

                if (!string.IsNullOrWhiteSpace(ackClientMessageId))
                {
                    // n8n/provider should echo our idempotency key (frontend externalMessageId) in clientMessageId.
                    // Older flows used an internal Message.Id (GUID). Support both:
                    // - Prefer ExternalMessageId match (string)
                    // - Fallback to Message.Id when clientMessageId parses as GUID
                    Message? outboundForAck = await (
                        from m in db.Messages
                        join c in db.Conversations on m.ConversationId equals c.Id
                        where m.ExternalMessageId == ackClientMessageId
                              && m.Direction == MessageDirection.Outbound
                              && c.IntegrationId == integration.Id
                              && c.CompanyId == companyId
                        select m).SingleOrDefaultAsync(cancellationToken);

                    if (outboundForAck is null && Guid.TryParse(ackClientMessageId, out var ackGuid))
                    {
                        outboundForAck = await (
                            from m in db.Messages
                            join c in db.Conversations on m.ConversationId equals c.Id
                            where m.Id == ackGuid
                                  && m.Direction == MessageDirection.Outbound
                                  && c.IntegrationId == integration.Id
                                  && c.CompanyId == companyId
                            select m).SingleOrDefaultAsync(cancellationToken);
                    }

                    if (outboundForAck is not null)
                    {
                        var providerIdTaken = await db.Messages
                            .AsNoTracking()
                            .AnyAsync(
                                m => m.ExternalMessageId == messagePayload.Id && m.Id != outboundForAck.Id,
                                cancellationToken);

                        outboundForAck.Status = MessageStatus.Sent;
                        outboundForAck.Metadata = MessageMetadataMerge.WithLastInboundProviderEvent(
                            outboundForAck.Metadata,
                            request.Event,
                            JsonOptions);
                        outboundForAck.UpdatedAt = DateTimeOffset.UtcNow;
                        if (!providerIdTaken)
                            outboundForAck.ExternalMessageId = messagePayload.Id;

                        await db.SaveChangesAsync(cancellationToken);
                        await tx.CommitAsync(cancellationToken);

                        SetSuccess(result, InboundProcessingOutcome.Persisted, "updated_outbound_by_client_message_id");

                        await TryPublishMessageStatusUpdatedAsync(
                            lookup.TenantId,
                            outboundForAck.Id,
                            outboundForAck.ConversationId,
                            outboundForAck.Status,
                            failureMessage: null,
                            cancellationToken);

                        return;
                    }

                    tenantLogger.LogWarning(
                        "Inbound outbound ack: no message for clientMessageId {ClientMessageId} integrationId {IntegrationId} externalIdentifier {ExternalIdentifier} providerMessageId {ProviderMessageId}",
                        ackClientMessageId,
                        integration.Id,
                        externalIdentifier,
                        messagePayload.Id);
                    SetPermanent(result, "outbound_ack_message_not_found");
                    return;
                }

                var phone = PhoneNumberNormalizer.ToNormalizedDigits(
                    request.Event.Identifications?.Contact?.PhoneNumber,
                    "BR");
                var name = request.Event.Identifications?.Contact?.Name?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(phone))
                {
                    tenantLogger.LogWarning(
                        "Inbound event ignored: missing contact phoneNumber. integrationId {IntegrationId} externalIdentifier {ExternalIdentifier} messageId {MessageId}",
                        integration.Id,
                        externalIdentifier,
                        messagePayload.Id);
                    SetPermanent(result, "missing_contact_phone");
                    return;
                }

                var contact = await db.Contacts
                    .SingleOrDefaultAsync(c => c.CompanyId == companyId && c.Phone == phone, cancellationToken);

                if (contact == null)
                {
                    contact = new Contact
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId,
                        Name = string.IsNullOrWhiteSpace(name) ? phone : name,
                        Phone = phone,
                        Email = string.Empty,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    db.Contacts.Add(contact);
                    await db.SaveChangesAsync(cancellationToken);
                }

                var eventTimestamp = messagePayload.Timestamp > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(messagePayload.Timestamp)
                    : DateTimeOffset.UtcNow;

                var conversation = await db.Conversations
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync(
                        c => c.CompanyId == companyId &&
                             c.ContactId == contact.Id &&
                             c.IntegrationId == integration.Id,
                        cancellationToken);

                if (conversation == null)
                {
                    var inboxId = await ResolveInboxIdAsync(
                        db,
                        settingsResolver,
                        integration,
                        companyId,
                        cancellationToken);

                    conversation = new Conversation
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId,
                        ContactId = contact.Id,
                        IntegrationId = integration.Id,
                        InboxId = inboxId,
                        AssignedAgentId = null,
                        AssignedAt = null,
                        Priority = 0,
                        Status = ConversationStatus.Open,
                        LastMessage = string.Empty,
                        LastMessageAt = eventTimestamp,
                        CreatedAt = DateTimeOffset.UtcNow
                    };

                    db.Conversations.Add(conversation);
                    await db.SaveChangesAsync(cancellationToken);

                    try
                    {
                        var autoAssign = sp.GetRequiredService<IConversationAutoAssignService>();
                        await autoAssign.TryAssignNewConversationAsync(companyId, lookup.TenantId, conversation, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        tenantLogger.LogWarning(ex, "Auto-assign failed for new conversation {ConversationId}", conversation.Id);
                    }

                    var conversationCreatedEvent = new RealtimeEvent<ConversationCreatedPayloadDto>
                    {
                        Type = "conversation.created",
                        TenantId = lookup.TenantId,
                        Timestamp = DateTime.UtcNow,
                        Payload = new ConversationCreatedPayloadDto
                        {
                            Id = conversation.Id,
                            InboxId = conversation.InboxId,
                            CreatedAt = conversation.CreatedAt,
                            ContactId = contact.Id,
                            ContactName = contact.Name ?? string.Empty
                        }
                    };

                    try
                    {
                        await realtimeService.SendToInboxAsync(conversation.InboxId.ToString(), conversationCreatedEvent);
                    }
                    catch (Exception ex)
                    {
                        tenantLogger.LogWarning(ex, "Failed to send conversation.created event via SignalR.");
                    }
                }

                var exists = await db.Messages
                    .AsNoTracking()
                    .AnyAsync(m => m.ExternalMessageId == messagePayload.Id, cancellationToken);

                if (exists)
                {
                    await CommitDuplicateAsync(
                        db,
                        realtimeService,
                        result,
                        lookup.TenantId,
                        integration.Id,
                        externalIdentifier,
                        messagePayload,
                        eventTimestamp,
                        conversation,
                        tenantLogger,
                        tx,
                        cancellationToken);
                    return;
                }

                var messageType = MapMessageType(messagePayload.Type);
                var direction = MapDirection(messagePayload.Direction);
                var content = ResolveContent(messagePayload);
                var resolvedMedia = messagePayload.ResolveMedia();
                var mediaProcessingStatus = IsAsyncMediaType(messageType) ? "processing" : "completed";

                var metadataJson = JsonSerializer.Serialize(new
                {
                    inboundEvent = request.Event,
                    mediaProcessingStatus
                }, JsonOptions);

                var persistNow = DateTimeOffset.UtcNow;
                var message = new Message
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    ExternalMessageId = messagePayload.Id,
                    Direction = direction,
                    Type = messageType,
                    Content = content,
                    ReplyToMessageId = null,
                    Status = MessageStatus.Sent,
                    Metadata = metadataJson,
                    SenderUserId = null,
                    MessageDate = eventTimestamp,
                    CreatedAt = persistNow,
                    UpdatedAt = persistNow
                };

                db.Messages.Add(message);
                TryAttachInboundMedia(message, messagePayload, db);

                conversation.LastMessage = content;
                if (conversation.LastMessageAt < eventTimestamp)
                    conversation.LastMessageAt = eventTimestamp;
                conversation.Status = ConversationStatus.Open;

                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (IsUniqueExternalMessageIdViolation(ex))
                {
                    tenantLogger.LogInformation(
                        ex,
                        "Inbound idempotent race on ExternalMessageId; reconciling as duplicate. messageId {MessageId}",
                        messagePayload.Id);

                    await tx.RollbackAsync(cancellationToken);

                    await using var tx2 = await db.Database.BeginTransactionAsync(cancellationToken);
                    conversation = await db.Conversations
                        .FirstAsync(c => c.Id == conversation.Id, cancellationToken);

                    await CommitDuplicateAsync(
                        db,
                        realtimeService,
                        result,
                        lookup.TenantId,
                        integration.Id,
                        externalIdentifier,
                        messagePayload,
                        eventTimestamp,
                        conversation,
                        tenantLogger,
                        tx2,
                        cancellationToken);
                    return;
                }

                await tx.CommitAsync(cancellationToken);

                SetSuccess(result, InboundProcessingOutcome.Persisted, "processed");

                if (resolvedMedia is not null &&
                    !string.IsNullOrWhiteSpace(resolvedMedia.Url) &&
                    IsAsyncMediaType(messageType))
                {
                    await mediaProcessingJobPublisher.PublishAsync(new MediaProcessingRequested
                    {
                        TenantId = lookup.TenantId,
                        MessageId = message.Id,
                        ConversationId = message.ConversationId,
                        IntegrationId = integration.Id,
                        MediaType = messagePayload.Type,
                        TempUrl = resolvedMedia.Url.Trim(),
                        MimeType = string.IsNullOrWhiteSpace(resolvedMedia.MimeType) ? null : resolvedMedia.MimeType.Trim(),
                        MessageDate = message.MessageDate,
                        Caption = string.IsNullOrWhiteSpace(resolvedMedia.Caption) ? null : resolvedMedia.Caption.Trim(),
                        ExternalMessageId = messagePayload.Id,
                        OriginalFileName = string.IsNullOrWhiteSpace(resolvedMedia.FileName) ? null : resolvedMedia.FileName.Trim(),
                        Animated = resolvedMedia.Animated,
                        Voice = resolvedMedia.Voice
                    }, cancellationToken);
                }

                var messageCreatedEvent = new RealtimeEvent<MessageCreatedPayloadDto>
                {
                    Type = "message.created",
                    TenantId = lookup.TenantId,
                    Timestamp = DateTime.UtcNow,
                    Payload = new MessageCreatedPayloadDto
                    {
                        Id = message.Id,
                        ConversationId = message.ConversationId,
                        ContactId = contact.Id,
                        ContactName = contact.Name,
                        Text = message.Content,
                        Type = message.Type.ToString().ToLowerInvariant(),
                        Media = resolvedMedia is null
                            ? null
                            : new MessageMediaListItemDto
                            {
                                Url = string.Empty,
                                MimeType = (resolvedMedia.MimeType ?? string.Empty).Trim(),
                                FileName = string.IsNullOrWhiteSpace(resolvedMedia.FileName) ? null : resolvedMedia.FileName.Trim(),
                                Caption = string.IsNullOrWhiteSpace(resolvedMedia.Caption) ? null : resolvedMedia.Caption.Trim()
                            },
                        SenderId = message.SenderUserId?.ToString(),
                        MessageDate = message.MessageDate,
                        CreatedAt = message.MessageDate,
                        Status = MessageListItemDtoMapper.ToStatusString(message.Status),
                        MediaProcessingStatus = mediaProcessingStatus
                    }
                };

                var movedEvent = new RealtimeEvent<ConversationMovedToTopPayloadDto>
                {
                    Type = "conversation.moved_to_top",
                    TenantId = lookup.TenantId,
                    Timestamp = DateTime.UtcNow,
                    Payload = new ConversationMovedToTopPayloadDto
                    {
                        Id = conversation.Id,
                        LastMessage = content,
                        LastMessageAt = conversation.LastMessageAt
                    }
                };

                try
                {
                    await realtimeService.SendToInboxAsync(conversation.InboxId.ToString(), movedEvent);
                }
                catch (Exception ex)
                {
                    tenantLogger.LogWarning(ex, "Failed to send moved_to_top event via SignalR.");
                }

                try
                {
                    await realtimeService.SendToConversationAsync(conversation.Id.ToString(), messageCreatedEvent);
                }
                catch (Exception ex)
                {
                    tenantLogger.LogWarning(ex, "Failed to send message.created event via SignalR.");
                }

                tenantLogger.LogInformation(
                    "Inbound message persisted. integrationId {IntegrationId} externalIdentifier {ExternalIdentifier} messageId {MessageId} conversationId {ConversationId} contactId {ContactId}",
                    integration.Id,
                    externalIdentifier,
                    messagePayload.Id,
                    conversation.Id,
                    contact.Id);
            });
        });

        return result;
    }

    private async Task HandleStatusEventAsync(
        ProcessInboundEventCommand request,
        SingleResponse<InboundProcessingResultDto> result,
        string provider,
        IntegrationType integrationType,
        string externalIdentifier,
        CancellationToken cancellationToken)
    {
        InboundStatusPayload? statusPayload;

        try
        {
            statusPayload = request.Event.Payload.Deserialize<InboundStatusPayload>(JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Inbound status event ignored: failed to deserialize payload");
            SetPermanent(result, "invalid_status_payload");
            return;
        }

        if (statusPayload == null)
        {
            SetPermanent(result, "missing_message_id");
            return;
        }

        var providerMessageId = ResolveStatusProviderMessageId(statusPayload);
        var clientMessageId = string.IsNullOrWhiteSpace(statusPayload.ClientMessageId)
            ? null
            : statusPayload.ClientMessageId.Trim();

        if (string.IsNullOrWhiteSpace(providerMessageId) && string.IsNullOrWhiteSpace(clientMessageId))
        {
            SetPermanent(result, "missing_message_id");
            return;
        }

        if (!TryMapProviderStatus(statusPayload.Status, out var newStatus))
        {
            logger.LogWarning(
                "Inbound status event ignored: unknown status {Status} for providerMessageId {ProviderMessageId} clientMessageId {ClientMessageId}",
                statusPayload.Status,
                providerMessageId,
                clientMessageId);
            SetPermanent(result, "unknown_status");
            return;
        }

        var lookup = await integrationLookupRepository.GetByProviderAndExternalIdentifierAsync(
            integrationType,
            externalIdentifier,
            cancellationToken);

        if (lookup == null)
        {
            logger.LogWarning(
                "Inbound status: integration lookup not found for provider {Provider} externalIdentifier {ExternalIdentifier} providerMessageId {ProviderMessageId} clientMessageId {ClientMessageId}",
                provider,
                externalIdentifier,
                providerMessageId,
                clientMessageId);
            SetPermanent(result, "integration_not_found");
            return;
        }

        await crossTenantService.ExecuteInTenantContextForSystemAsync(lookup.TenantId, async sp =>
        {
            var db = sp.GetRequiredService<IApplicationDbContext>();
            var tenantLogger = sp.GetRequiredService<ILogger<ProcessInboundEventCommandHandler>>();

            var executionStrategy = db.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

                var integration = await db.Integrations
                    .SingleOrDefaultAsync(i => i.Id == lookup.IntegrationId, cancellationToken);

                if (integration == null)
                {
                    tenantLogger.LogWarning(
                        "Inbound status: integration not found in tenant db. tenantId {TenantId} integrationId {IntegrationId}",
                        lookup.TenantId,
                        lookup.IntegrationId);
                    SetPermanent(result, "integration_missing_in_tenant");
                    return;
                }

                var companyId = integration.CompanyId;

                // clientMessageId here is the outbound ExternalMessageId (idempotency key sent by the frontend),
                // NOT the internal Message.Id.
                Message? message = null;
                if (!string.IsNullOrWhiteSpace(clientMessageId))
                {
                    message = await (
                        from m in db.Messages
                        join c in db.Conversations on m.ConversationId equals c.Id
                        where m.ExternalMessageId == clientMessageId
                              && c.IntegrationId == integration.Id
                              && c.CompanyId == companyId
                              && m.Direction == MessageDirection.Outbound
                        select m).SingleOrDefaultAsync(cancellationToken);
                }

                if (message == null && !string.IsNullOrWhiteSpace(providerMessageId))
                {
                    message = await (
                        from m in db.Messages
                        join c in db.Conversations on m.ConversationId equals c.Id
                        where m.ExternalMessageId == providerMessageId!.Trim()
                              && c.IntegrationId == integration.Id
                              && c.CompanyId == companyId
                              && m.Direction == MessageDirection.Outbound
                        select m).SingleOrDefaultAsync(cancellationToken);
                }

                if (message == null)
                {
                    tenantLogger.LogWarning(
                        "Inbound status: no outbound message for ExternalMessageId {ExternalMessageId} clientMessageId {ClientMessageId} integrationId {IntegrationId}",
                        providerMessageId,
                        clientMessageId,
                        integration.Id);
                    // Permanent: message may never exist (old events, wrong id); retry would not help.
                    SetPermanent(result, "status_message_not_found");
                    return;
                }

                if (!ShouldApplyStatusUpdate(message.Status, newStatus))
                {
                    await tx.CommitAsync(cancellationToken);
                    SetSuccess(result, InboundProcessingOutcome.DuplicateIgnored, "status_not_advanced");
                    return;
                }

                message.Status = newStatus;
                message.Metadata = MessageMetadataMerge.WithLastInboundProviderEvent(
                    message.Metadata,
                    request.Event,
                    JsonOptions);
                message.UpdatedAt = DateTimeOffset.UtcNow;

                await db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                SetSuccess(result, InboundProcessingOutcome.Persisted, "status_updated");

                tenantLogger.LogInformation(
                    "Inbound status applied. integrationId {IntegrationId} externalMessageId {ExternalMessageId} clientMessageId {ClientMessageId} status {Status}",
                    integration.Id,
                    message.ExternalMessageId,
                    message.Id,
                    newStatus);

                var failureMessage = newStatus == MessageStatus.Failed
                    ? ResolveStatusFailureMessage(request.Event.Payload)
                    : null;

                await TryPublishMessageStatusUpdatedAsync(
                    lookup.TenantId,
                    message.Id,
                    message.ConversationId,
                    message.Status,
                    failureMessage,
                    cancellationToken);
            });
        });
    }

    private async Task TryPublishMessageStatusUpdatedAsync(
        string tenantId,
        Guid messageId,
        Guid conversationId,
        MessageStatus status,
        string? failureMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var evt = new RealtimeEvent<MessageStatusUpdatedPayloadDto>
            {
                Type = "message.status_updated",
                TenantId = tenantId,
                Timestamp = DateTime.UtcNow,
                Payload = new MessageStatusUpdatedPayloadDto
                {
                    Id = messageId,
                    ConversationId = conversationId,
                    Status = MessageListItemDtoMapper.ToStatusString(status),
                    FailureMessage = string.IsNullOrWhiteSpace(failureMessage) ? null : failureMessage.Trim()
                }
            };

            await realtimeService.SendToConversationAsync(conversationId.ToString("D"), evt);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send message.status_updated via SignalR.");
        }
    }

    private static string? ResolveStatusProviderMessageId(InboundStatusPayload payload)
    {
        foreach (var candidate in new[] { payload.MessageId, payload.Id })
        {
            var t = candidate?.Trim();
            if (!string.IsNullOrWhiteSpace(t))
                return t;
        }

        return null;
    }

    private static string? ResolveStatusFailureMessage(JsonElement payload)
    {
        // Expected (example):
        // payload.error.error.message
        try
        {
            if (payload.ValueKind != JsonValueKind.Object)
                return null;

            if (!payload.TryGetProperty("error", out var e1))
                return null;

            if (e1.ValueKind == JsonValueKind.Object &&
                e1.TryGetProperty("error", out var e2) &&
                e2.ValueKind == JsonValueKind.Object &&
                e2.TryGetProperty("message", out var msgEl))
            {
                var msg = msgEl.GetString();
                return string.IsNullOrWhiteSpace(msg) ? null : msg;
            }

            // Fallbacks
            if (e1.ValueKind == JsonValueKind.Object && e1.TryGetProperty("message", out var msgEl2))
            {
                var msg = msgEl2.GetString();
                return string.IsNullOrWhiteSpace(msg) ? null : msg;
            }
        }
        catch
        {
            // ignore parse issues; do not break status handling
        }

        return null;
    }

    private static bool TryMapProviderStatus(string? status, out MessageStatus mapped)
    {
        mapped = default;
        if (string.IsNullOrWhiteSpace(status))
            return false;

        switch (status.Trim().ToLowerInvariant())
        {
            case "sent":
                mapped = MessageStatus.Sent;
                return true;
            case "delivered":
                mapped = MessageStatus.Delivered;
                return true;
            case "read":
                mapped = MessageStatus.Read;
                return true;
            case "failed":
                mapped = MessageStatus.Failed;
                return true;
            default:
                return false;
        }
    }

    private static int StatusRank(MessageStatus status) =>
        status switch
        {
            MessageStatus.Pending => 0,
            MessageStatus.Sent => 1,
            MessageStatus.Delivered => 2,
            MessageStatus.Read => 3,
            MessageStatus.Failed => 4,
            _ => 0
        };

    private static bool ShouldApplyStatusUpdate(MessageStatus current, MessageStatus incoming)
    {
        if (incoming == MessageStatus.Failed)
            return current != MessageStatus.Failed;

        if (current == MessageStatus.Failed)
            return false;

        return StatusRank(incoming) > StatusRank(current);
    }

    private static void SetPermanent(SingleResponse<InboundProcessingResultDto> result, string reason)
    {
        result.Data!.Outcome = InboundProcessingOutcome.PermanentRejected;
        result.Data!.Reason = reason;
        result.Data!.Processed = false;
    }

    private static void SetSuccess(
        SingleResponse<InboundProcessingResultDto> result,
        InboundProcessingOutcome outcome,
        string reason)
    {
        result.Data!.Outcome = outcome;
        result.Data!.Reason = reason;
        result.Data!.Processed = true;
    }

    private static bool IsUniqueExternalMessageIdViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("UX_Message_ExternalMessageId", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("IX_Message_ExternalMessageId", StringComparison.OrdinalIgnoreCase);
    }

    private async Task CommitDuplicateAsync(
        IApplicationDbContext db,
        IRealtimeService realtimeService,
        SingleResponse<InboundProcessingResultDto> result,
        string tenantId,
        Guid integrationId,
        string externalIdentifier,
        InboundMessagePayload messagePayload,
        DateTimeOffset eventTimestamp,
        Conversation conversation,
        ILogger tenantLogger,
        IDbContextTransaction tx,
        CancellationToken cancellationToken)
    {
        tenantLogger.LogInformation(
            "Inbound duplicate message ignored. integrationId {IntegrationId} externalIdentifier {ExternalIdentifier} messageId {MessageId}",
            integrationId,
            externalIdentifier,
            messagePayload.Id);

        var movedToTop = conversation.LastMessageAt < eventTimestamp;
        var lastMessage = ResolveContent(messagePayload);
        if (movedToTop)
        {
            conversation.LastMessageAt = eventTimestamp;
            conversation.LastMessage = lastMessage;
        }
        conversation.Status = ConversationStatus.Open;

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        SetSuccess(result, InboundProcessingOutcome.DuplicateIgnored, "duplicate_ignored");

        if (movedToTop)
        {
            var movedEventDuplicate = new RealtimeEvent<ConversationMovedToTopPayloadDto>
            {
                Type = "conversation.moved_to_top",
                TenantId = tenantId,
                Timestamp = DateTime.UtcNow,
                Payload = new ConversationMovedToTopPayloadDto
                {
                    Id = conversation.Id,
                    LastMessage = lastMessage,
                    LastMessageAt = conversation.LastMessageAt
                }
            };

            try
            {
                await realtimeService.SendToInboxAsync(conversation.InboxId.ToString(), movedEventDuplicate);
            }
            catch (Exception ex)
            {
                tenantLogger.LogWarning(ex, "Failed to send moved_to_top event via SignalR.");
            }
        }
    }

    private static bool TryMapProvider(string provider, out IntegrationType type)
    {
        if (provider.Equals("whatsapp", StringComparison.OrdinalIgnoreCase))
        {
            type = IntegrationType.WhatsApp;
            return true;
        }

        type = default;
        return false;
    }

    private static MessageDirection MapDirection(string? direction)
    {
        return direction?.Equals("outbound", StringComparison.OrdinalIgnoreCase) == true
            ? MessageDirection.Outbound
            : MessageDirection.Inbound;
    }

    private static MessageType MapMessageType(string? type)
    {
        return type?.Trim().ToLowerInvariant() switch
        {
            "image" => MessageType.Image,
            "video" => MessageType.Video,
            "audio" => MessageType.Audio,
            "document" => MessageType.Document,
            "sticker" => MessageType.Sticker,
            "reaction" => MessageType.Reaction,
            "template" => MessageType.Template,
            _ => MessageType.Text
        };
    }

    private static MediaType? MapMediaType(string? type)
    {
        return type?.Trim().ToLowerInvariant() switch
        {
            "image" => MediaType.Image,
            "video" => MediaType.Video,
            "audio" => MediaType.Audio,
            "document" => MediaType.Document,
            "sticker" => MediaType.Sticker,
            _ => null
        };
    }

    private static void TryAttachInboundMedia(
        Message message,
        InboundMessagePayload payload,
        IApplicationDbContext db)
    {
        var mediaType = MapMediaType(payload.Type);
        if (!mediaType.HasValue)
            return;

        var media = payload.ResolveMedia();
        var url = media?.Url?.Trim();
        if (string.IsNullOrWhiteSpace(url))
            return;

        db.MessageMedia.Add(new MessageMedia
        {
            Id = Guid.NewGuid(),
            MessageId = message.Id,
            MediaType = mediaType.Value,
            Url = url,
            MimeType = (media?.MimeType ?? string.Empty).Trim(),
            FileSize = 0,
            FileName = string.IsNullOrWhiteSpace(media?.FileName) ? null : media!.FileName!.Trim(),
            Animated = media?.Animated ?? false,
            Voice = media?.Voice ?? false,
            Caption = string.IsNullOrWhiteSpace(media?.Caption) ? null : media!.Caption!.Trim()
        });
    }

    private static string ResolveContent(InboundMessagePayload payload)
    {
        if (payload.Type.Equals("text", StringComparison.OrdinalIgnoreCase))
            return payload.Text?.Body?.Trim() ?? string.Empty;

        var caption = payload.ResolveMedia()?.Caption?.Trim();
        if (!string.IsNullOrWhiteSpace(caption))
            return caption;

        return ResolveMediaPlaceholder(payload.Type);
    }

    private static string ResolveMediaPlaceholder(string? type)
    {
        return type?.Trim().ToLowerInvariant() switch
        {
            "image" => "[Image]",
            "video" => "[Video]",
            "audio" => "[Audio]",
            "document" => "[Document]",
            "sticker" => "[Sticker]",
            _ => "[Media]"
        };
    }

    private static bool IsAsyncMediaType(MessageType type) =>
        type is MessageType.Image or MessageType.Video or MessageType.Audio or MessageType.Document or MessageType.Sticker;

    private static async Task<Guid> ResolveInboxIdAsync(
        IApplicationDbContext db,
        IIntegrationSettingsResolver settingsResolver,
        Integration integration,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        Guid? configuredInboxId = null;

        if (integration.Type == IntegrationType.WhatsApp)
        {
            var settings = (WhatsAppSettings)settingsResolver.Deserialize(integration.Type, integration.Settings);
            configuredInboxId = settings.InboxId;
        }

        if (configuredInboxId.HasValue)
        {
            var exists = await db.Inboxes
                .AsNoTracking()
                .AnyAsync(i => i.Id == configuredInboxId.Value && i.CompanyId == companyId, cancellationToken);
            if (exists)
                return configuredInboxId.Value;
        }

        var firstActiveInboxId = await db.Inboxes
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId && i.IsActive)
            .OrderBy(i => i.CreatedAt)
            .Select(i => i.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (firstActiveInboxId != Guid.Empty)
            return firstActiveInboxId;

        var inbox = new Inbox
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = "Inbox padrÃ£o",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Inboxes.Add(inbox);
        await db.SaveChangesAsync(cancellationToken);
        return inbox.Id;
    }
}

