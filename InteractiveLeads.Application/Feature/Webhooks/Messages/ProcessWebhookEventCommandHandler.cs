using System.Text.Json;
using InteractiveLeads.Application.Integrations.Settings;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Realtime.Models;
using InteractiveLeads.Application.Realtime.Services;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InteractiveLeads.Application.Feature.Webhooks.Messages;

public sealed class ProcessWebhookEventCommandHandler(
    IIntegrationExternalIdentifierLookupRepository integrationLookupRepository,
    ICrossTenantService crossTenantService,
    IRealtimeService realtimeService,
    ILogger<ProcessWebhookEventCommandHandler> logger)
    : IRequestHandler<ProcessWebhookEventCommand, IResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<IResponse> Handle(ProcessWebhookEventCommand request, CancellationToken cancellationToken)
    {
        var result = new SingleResponse<WebhookProcessingResultDto>(new WebhookProcessingResultDto { Processed = false });

        var provider = request.Event.Provider?.Trim();
        var eventType = request.Event.EventType?.Trim();
        var externalIdentifier = request.Event.Identifications?.ExternalIdentifier?.Trim();

        if (string.IsNullOrWhiteSpace(provider) ||
            string.IsNullOrWhiteSpace(eventType) ||
            string.IsNullOrWhiteSpace(externalIdentifier))
        {
            result.Data!.Reason = "invalid_payload";
            return result;
        }

        if (!TryMapProvider(provider, out var integrationType))
        {
            logger.LogWarning("Webhook ignored: unsupported provider {Provider}", provider);
            result.Data!.Reason = "unsupported_provider";
            return result;
        }

        if (!eventType.Equals("message", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Webhook received but not implemented for eventType {EventType}", eventType);
            result.Data!.Reason = "event_type_not_implemented";
            return result;
        }

        WebhookMessagePayload? messagePayload;

        try
        {
            messagePayload = request.Event.Payload.Deserialize<WebhookMessagePayload>(JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Webhook ignored: failed to deserialize message payload");
            result.Data!.Reason = "invalid_message_payload";
            return result;
        }

        if (messagePayload == null || string.IsNullOrWhiteSpace(messagePayload.Id))
        {
            result.Data!.Reason = "missing_message_id";
            return result;
        }

        var lookup = await integrationLookupRepository.GetByProviderAndExternalIdentifierAsync(
            integrationType,
            externalIdentifier,
            cancellationToken);

        if (lookup == null)
        {
            logger.LogWarning(
                "Webhook ignored: integration lookup not found for provider {Provider} externalIdentifier {ExternalIdentifier} messageId {MessageId}",
                provider,
                externalIdentifier,
                messagePayload.Id);

            result.Data!.Reason = "integration_not_found";
            return result;
        }

        await crossTenantService.ExecuteInTenantContextForSystemAsync(lookup.TenantId, async sp =>
        {
            var db = sp.GetRequiredService<IApplicationDbContext>();
            var settingsResolver = sp.GetRequiredService<IIntegrationSettingsResolver>();
            var tenantLogger = sp.GetRequiredService<ILogger<ProcessWebhookEventCommandHandler>>();

            var executionStrategy = db.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

                var integration = await db.Integrations
                    .SingleOrDefaultAsync(i => i.Id == lookup.IntegrationId, cancellationToken);

                if (integration == null)
                {
                    tenantLogger.LogWarning(
                        "Webhook ignored: integration not found in tenant db. tenantId {TenantId} integrationId {IntegrationId} externalIdentifier {ExternalIdentifier} messageId {MessageId}",
                        lookup.TenantId,
                        lookup.IntegrationId,
                        externalIdentifier,
                        messagePayload.Id);
                    return;
                }

                var companyId = integration.CompanyId;

                var phone = request.Event.Identifications.Contact?.PhoneNumber?.Trim() ?? string.Empty;
                var name = request.Event.Identifications.Contact?.Name?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(phone))
                {
                    tenantLogger.LogWarning(
                        "Webhook ignored: missing contact phoneNumber. integrationId {IntegrationId} externalIdentifier {ExternalIdentifier} messageId {MessageId}",
                        integration.Id,
                        externalIdentifier,
                        messagePayload.Id);
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

                // Prefer reusing the most recent conversation for the contact+integration.
                // New incoming/outgoing messages should "revive" a closed conversation rather than creating duplicates.
                var isNewConversation = false;
                var conversation = await db.Conversations
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync(
                        c => c.CompanyId == companyId &&
                             c.ContactId == contact.Id &&
                             c.IntegrationId == integration.Id,
                        cancellationToken);

                if (conversation == null)
                {
                    isNewConversation = true;
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
                    tenantLogger.LogInformation(
                        "Webhook duplicate message ignored. integrationId {IntegrationId} externalIdentifier {ExternalIdentifier} messageId {MessageId}",
                        integration.Id,
                        externalIdentifier,
                        messagePayload.Id);

                    // Even if we already have the message, ensure conversation's LastMessageAt is not stale.
                    var movedToTop = conversation.LastMessageAt < eventTimestamp;
                    if (movedToTop)
                        conversation.LastMessageAt = eventTimestamp;
                    conversation.Status = ConversationStatus.Open;

                    await db.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);

                    if (movedToTop)
                    {
                        var lastMessage = ResolveContent(messagePayload);

                        var movedEventDuplicate = new RealtimeEvent<ConversationMovedToTopPayloadDto>
                        {
                            Type = "conversation.moved_to_top",
                            TenantId = lookup.TenantId,
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

                await db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

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

                // Always notify the inbox list after inserting a new message.
                // This keeps `lastMessage` and `lastMessageAt` in sync on the UI,
                // even if LastMessageAt didn't advance strictly (timestamp equality).
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
                    "Webhook message processed. integrationId {IntegrationId} externalIdentifier {ExternalIdentifier} messageId {MessageId} conversationId {ConversationId} contactId {ContactId}",
                    integration.Id,
                    externalIdentifier,
                    messagePayload.Id,
                    conversation.Id,
                    contact.Id);
            });
        });

        result.Data!.Processed = true;
        result.Data!.Reason = "processed";
        return result;
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

    private static string ResolveContent(WebhookMessagePayload payload)
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

