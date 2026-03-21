using System.Text.Json;
using System.Text.Json.Serialization;
using InteractiveLeads.Application.Feature.Inbound;
using InteractiveLeads.Application.Integrations.Settings;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Realtime.Models;
using InteractiveLeads.Application.Realtime.Services;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InteractiveLeads.Application.Feature.Inbound.Messages;

public sealed class ProcessInboundEventCommandHandler(
    IIntegrationExternalIdentifierLookupRepository integrationLookupRepository,
    ICrossTenantService crossTenantService,
    IRealtimeService realtimeService,
    ILogger<ProcessInboundEventCommandHandler> logger)
    : IRequestHandler<ProcessInboundEventCommand, IResponse>
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

            ThrowOrTransientRetry(request, result, "integration_not_found");
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

                    ThrowOrTransientRetry(request, result, "integration_missing_in_tenant");
                    return;
                }

                var companyId = integration.CompanyId;

                var phone = request.Event.Identifications?.Contact?.PhoneNumber?.Trim() ?? string.Empty;
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
                        LastMessageAt = eventTimestamp,
                        CreatedAt = DateTimeOffset.UtcNow
                    };

                    db.Conversations.Add(conversation);
                    await db.SaveChangesAsync(cancellationToken);

                    var conversationCreatedEvent = new RealtimeEvent<ConversationCreatedPayloadDto>
                    {
                        Type = "conversation.created",
                        TenantId = lookup.TenantId,
                        Timestamp = DateTime.UtcNow,
                        Payload = new ConversationCreatedPayloadDto
                        {
                            Id = conversation.Id,
                            InboxId = conversation.InboxId,
                            CreatedAt = conversation.CreatedAt
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

                var metadataJson = JsonSerializer.Serialize(request.Event, JsonOptions);

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
                    CreatedAt = eventTimestamp
                };

                db.Messages.Add(message);

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

                var messageCreatedEvent = new RealtimeEvent<MessageCreatedPayloadDto>
                {
                    Type = "message.created",
                    TenantId = lookup.TenantId,
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

    private static void ThrowOrTransientRetry(
        ProcessInboundEventCommand request,
        SingleResponse<InboundProcessingResultDto> result,
        string reasonCode)
    {
        if (request.ReliableMessaging)
            throw new InboundTransientException(reasonCode);

        result.Data!.Outcome = InboundProcessingOutcome.TransientRetry;
        result.Data!.Reason = reasonCode;
        result.Data!.Processed = false;
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
        if (movedToTop)
            conversation.LastMessageAt = eventTimestamp;
        conversation.Status = ConversationStatus.Open;

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        SetSuccess(result, InboundProcessingOutcome.DuplicateIgnored, "duplicate_ignored");

        if (movedToTop)
        {
            var lastMessage = ResolveContent(messagePayload);

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

    private static string ResolveContent(InboundMessagePayload payload)
    {
        if (payload.Type.Equals("text", StringComparison.OrdinalIgnoreCase))
            return payload.Text?.Body?.Trim() ?? string.Empty;

        return payload.Media?.Caption?.Trim()
               ?? payload.Media?.Url?.Trim()
               ?? string.Empty;
    }

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
            Name = "Inbox padrão",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Inboxes.Add(inbox);
        await db.SaveChangesAsync(cancellationToken);
        return inbox.Id;
    }
}
