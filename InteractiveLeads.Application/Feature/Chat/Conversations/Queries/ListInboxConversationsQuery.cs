using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Conversations.Queries;

public sealed class ListInboxConversationsQuery : IRequest<IResponse>
{
    public Guid InboxId { get; set; }
}

public sealed class PagedInboxConversationsQuery : IRequest<IResponse>
{
    public Guid? InboxId { get; set; }

    /// <summary>
    /// Optional cursor based on LastMessageAt. When provided, only conversations
    /// with LastMessageAt strictly less than this value will be returned.
    /// </summary>
    public DateTimeOffset? Cursor { get; set; }

    /// <summary>
    /// Maximum number of conversations to return in this page.
    /// </summary>
    public int PageSize { get; set; } = 30;
}

public sealed class ListInboxConversationsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IRequestHandler<ListInboxConversationsQuery, IResponse>
{
    public async Task<IResponse> Handle(ListInboxConversationsQuery request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);
        await ChatContext.EnsureInboxAccessAsync(db, currentUserService, request.InboxId, companyId, cancellationToken);

        var items = await db.Conversations
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.InboxId == request.InboxId)
            .OrderByDescending(c => c.LastMessageAt)
            .Select(c => new ConversationDto
            {
                Id = c.Id,
                InboxId = c.InboxId,
                ContactId = c.ContactId,
                Status = c.Status,
                AssignedAgentId = c.AssignedAgentId,
                LastMessageAt = c.LastMessageAt,
                Priority = c.Priority,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new ListResponse<ConversationDto>(items, items.Count);
    }
}

public sealed class PagedInboxConversationsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IRequestHandler<PagedInboxConversationsQuery, IResponse>
{
    public async Task<IResponse> Handle(PagedInboxConversationsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = request.PageSize <= 0 ? 30 : Math.Min(request.PageSize, 100);

        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);
        var query = ChatContext.ApplyConversationAccessFilter(
            db,
            currentUserService,
            companyId,
            db.Conversations.AsNoTracking());

        if (request.InboxId.HasValue)
        {
            await ChatContext.EnsureInboxAccessAsync(db, currentUserService, request.InboxId.Value, companyId, cancellationToken);
            query = query.Where(c => c.InboxId == request.InboxId.Value);
        }

        if (request.Cursor.HasValue)
        {
            query = query.Where(c => c.LastMessageAt < request.Cursor.Value);
        }

        var items = await query
            .OrderByDescending(c => c.LastMessageAt)
            .Take(pageSize + 1)
            .Select(c => new InboxConversationListItemDto
            {
                Id = c.Id,
                InboxId = c.InboxId,
                ContactId = c.ContactId,
                ContactName = c.Contact.Name,
                LastMessage = c.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Content)
                    .FirstOrDefault() ?? string.Empty,
                LastMessageAt = c.LastMessageAt,
                InboxName = c.Inbox.Name,
                Status = c.Status,
                AssignedAgentId = c.AssignedAgentId
            })
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items = items.Take(pageSize).ToList();
        }

        var nextCursor = hasMore
            ? items.Min(i => i.LastMessageAt)
            : (DateTimeOffset?)null;

        var response = new CursorListResponse<InboxConversationListItemDto>(items, hasMore, nextCursor);
        return response;
    }
}


