using System.Text.Json;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Realtime.Models;
using InteractiveLeads.Application.Realtime.Services;
using InteractiveLeads.Application.Interfaces.HttpRequests;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Feature.Chat.Messages;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Messages.Commands;

public sealed class SendConversationMessageCommand : IRequest<IResponse>
{
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ExternalMessageId { get; set; } = string.Empty;
}

public sealed class SendConversationMessageCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IRealtimeService realtimeService,
    IExternalApiHttpClientFactory externalApiHttpClientFactory) : IRequestHandler<SendConversationMessageCommand, IResponse>
{
    private const string MessageSenderApiName = "MessageSender";
    private const string SendMessagePath = "webhook-test/enviar-mensagem";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<IResponse> Handle(SendConversationMessageCommand request, CancellationToken cancellationToken)
    {
        var content = (request.Content ?? string.Empty).Trim();
        var externalMessageId = (request.ExternalMessageId ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Content is required.", "chat.message.content_required");
            throw new BadRequestException(response);
        }

        if (string.IsNullOrWhiteSpace(externalMessageId))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("ExternalMessageId is required.", "chat.message.external_id_required");
            throw new BadRequestException(response);
        }

        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);
        var tenantId = currentUserService.GetUserTenant();

        var conversation = await db.Conversations
            .Include(c => c.Inbox)
            .Include(c => c.Integration)
            .Include(c => c.Contact)
            .Where(c => c.Id == request.ConversationId && c.CompanyId == companyId)
            .SingleOrDefaultAsync(cancellationToken);

        if (conversation == null)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Conversation not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        await ChatContext.EnsureInboxAccessAsync(db, currentUserService, conversation.InboxId, companyId, cancellationToken);

        // MVP scope: for now only persist outbound messages for WhatsApp conversations.
        if (conversation.Integration.Type != IntegrationType.WhatsApp)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Integration type not implemented for outbound messages yet.", "chat.message.integration_not_supported");
            throw new BadRequestException(response);
        }

        var now = DateTimeOffset.UtcNow;
        var senderUserId = Guid.TryParse(currentUserService.GetUserId(), out var userGuid) ? userGuid : (Guid?)null;

        var existingMessage = await db.Messages
            .AsNoTracking()
            .Where(m =>
                m.ExternalMessageId == externalMessageId)
            .Select(m => new
            {
                m.Id,
                m.Content,
                m.CreatedAt,
                m.Direction
            })
            .SingleOrDefaultAsync(cancellationToken);

        // Deduplication: if the same outbound message was already created, only update conversation state.
        if (existingMessage != null)
        {
            var movedToTop = conversation.LastMessageAt < now;
            if (movedToTop)
                conversation.LastMessageAt = now;

            conversation.Status = ConversationStatus.Open;
            await db.SaveChangesAsync(cancellationToken);

            if (movedToTop)
            {
                var movedEventDedup = new RealtimeEvent<ConversationMovedToTopPayloadDto>
                {
                    Type = "conversation.moved_to_top",
                    TenantId = tenantId,
                    Timestamp = DateTime.UtcNow,
                    Payload = new ConversationMovedToTopPayloadDto
                    {
                        Id = conversation.Id,
                        LastMessage = existingMessage.Content,
                        LastMessageAt = conversation.LastMessageAt
                    }
                };

                await realtimeService.SendToInboxAsync(conversation.InboxId.ToString(), movedEventDedup);
            }

            return new SingleResponse<MessageListItemDto>(new MessageListItemDto
            {
                Id = existingMessage.Id,
                Content = existingMessage.Content,
                Direction = existingMessage.Direction == MessageDirection.Inbound ? "inbound" : "outbound",
                CreatedAt = existingMessage.CreatedAt
            });
        }

        var phone = (conversation.Contact.Phone ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(phone))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Contact phone is required for outbound external delivery.", "chat.message.phone_required");
            throw new BadRequestException(response);
        }

        var externalApiResponse = await externalApiHttpClientFactory
            .Create(MessageSenderApiName)
            .PostAsync(
                SendMessagePath,
                new
                {
                    phone,
                    message = content,
                    externalMessageId,
                    conversationId = conversation.Id.ToString()
                });

        if (externalApiResponse.HasAnyErrorMessage)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("External provider rejected message delivery.", "chat.message.external_send_failed");
            foreach (var item in externalApiResponse.Messages)
                response.Messages.Add(item);
            throw new BadRequestException(response);
        }

        var metadataJson = JsonSerializer.Serialize(
            new { integrationType = conversation.Integration.Type.ToString() },
            JsonOptions);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            ExternalMessageId = externalMessageId,
            Direction = MessageDirection.Outbound,
            Type = MessageType.Text,
            Content = content,
            ReplyToMessageId = null,
            Status = MessageStatus.Sent,
            Metadata = metadataJson,
            SenderUserId = senderUserId,
            CreatedAt = now
        };

        db.Messages.Add(message);

        var movedToTopCreated = conversation.LastMessageAt < now;
        if (movedToTopCreated)
            conversation.LastMessageAt = now;

        conversation.Status = ConversationStatus.Open;

        await db.SaveChangesAsync(cancellationToken);

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

        // Always notify the inbox list after we created a message.
        // Even if `LastMessageAt` didn't strictly advance (timestamp equality), the card must update
        // preview text and/or the date.
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

        return new SingleResponse<MessageListItemDto>(new MessageListItemDto
        {
            Id = message.Id,
            Content = message.Content,
            Direction = "outbound",
            CreatedAt = message.CreatedAt
        });
    }
}
