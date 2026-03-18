using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Messages.Queries;

public sealed class ListConversationMessagesQuery : IRequest<IResponse>
{
    public Guid ConversationId { get; set; }
    public DateTimeOffset? BeforeCreatedAt { get; set; }
    public int PageSize { get; set; } = 30;
}

public sealed class ListConversationMessagesQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IRequestHandler<ListConversationMessagesQuery, IResponse>
{
    public async Task<IResponse> Handle(ListConversationMessagesQuery request, CancellationToken cancellationToken)
    {
        var pageSize = request.PageSize <= 0 ? 30 : Math.Min(request.PageSize, 100);

        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var conversation = await db.Conversations
            .AsNoTracking()
            .Include(c => c.Inbox)
            .Where(c => c.Id == request.ConversationId && c.CompanyId == companyId)
            .SingleOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            var errorResponse = new ResultResponse();
            errorResponse.AddErrorMessage("Conversation not found.", "general.not_found");
            throw new NotFoundException(errorResponse);
        }

        await ChatContext.EnsureInboxAccessAsync(db, currentUserService, conversation.InboxId, companyId, cancellationToken);

        var messagesQuery = db.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == request.ConversationId);

        if (request.BeforeCreatedAt.HasValue)
        {
            messagesQuery = messagesQuery.Where(m => m.CreatedAt < request.BeforeCreatedAt.Value);
        }

        var items = await messagesQuery
            .OrderByDescending(m => m.CreatedAt)
            .Take(pageSize + 1)
            .Select(m => new MessageListItemDto
            {
                Id = m.Id,
                Content = m.Content,
                Direction = m.Direction == MessageDirection.Inbound ? "inbound" : "outbound",
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items = items.Take(pageSize).ToList();
        }

        // Order chronologically (oldest first) for chat UI
        items = items
            .OrderBy(m => m.CreatedAt)
            .ToList();

        var nextCursor = hasMore
            ? items.Min(m => m.CreatedAt)
            : (DateTimeOffset?)null;

        var response = new CursorListResponse<MessageListItemDto>(items, hasMore, nextCursor);
        return response;
    }
}

