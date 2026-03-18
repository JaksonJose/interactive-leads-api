using System.Text.Json;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
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
    ICurrentUserService currentUserService) : IRequestHandler<SendConversationMessageCommand, IResponse>
{
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

        var conversation = await db.Conversations
            .Include(c => c.Inbox)
            .Include(c => c.Integration)
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
            if (conversation.LastMessageAt < now)
                conversation.LastMessageAt = now;

            conversation.Status = ConversationStatus.Open;
            await db.SaveChangesAsync(cancellationToken);

            return new SingleResponse<MessageListItemDto>(new MessageListItemDto
            {
                Id = existingMessage.Id,
                Content = existingMessage.Content,
                Direction = existingMessage.Direction == MessageDirection.Inbound ? "inbound" : "outbound",
                CreatedAt = existingMessage.CreatedAt
            });
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

        if (conversation.LastMessageAt < now)
            conversation.LastMessageAt = now;

        conversation.Status = ConversationStatus.Open;

        await db.SaveChangesAsync(cancellationToken);

        return new SingleResponse<MessageListItemDto>(new MessageListItemDto
        {
            Id = message.Id,
            Content = message.Content,
            Direction = "outbound",
            CreatedAt = message.CreatedAt
        });
    }
}
