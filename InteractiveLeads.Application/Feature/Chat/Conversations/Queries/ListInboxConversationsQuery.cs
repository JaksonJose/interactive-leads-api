using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Conversations.Queries;

public sealed class ListInboxConversationsQuery : IApplicationRequest<IResponse>
{
    public Guid InboxId { get; set; }
}

public sealed class PagedInboxConversationsQuery : IApplicationRequest<IResponse>
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
    ICurrentUserService currentUserService) : IApplicationRequestHandler<ListInboxConversationsQuery, IResponse>
{
    public async Task<IResponse> Handle(ListInboxConversationsQuery request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);
        await ChatContext.EnsureInboxAccessAsync(db, currentUserService, request.InboxId, companyId, cancellationToken);

        var baseQuery = db.Conversations
            .AsNoTracking()
            .Where(c => c.InboxId == request.InboxId);

        var filtered = ChatContext.ApplyConversationAccessFilter(
            db,
            currentUserService,
            companyId,
            baseQuery);

        var items = await filtered
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
    ICurrentUserService currentUserService,
    IUserSummaryLookupService userSummaryLookup) : IApplicationRequestHandler<PagedInboxConversationsQuery, IResponse>
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
                LastMessage = c.LastMessage,
                LastMessageAt = c.LastMessageAt,
                CreatedAt = c.CreatedAt,
                InboxName = c.Inbox.Name,
                Status = c.Status,
                AssignedAgentId = c.AssignedAgentId
            })
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
        {
            var assignedUserIds = items
                .Where(i => i.AssignedAgentId.HasValue)
                .Select(i => i.AssignedAgentId!.Value.ToString())
                .Distinct()
                .ToList();

            if (assignedUserIds.Count > 0)
            {
                var summaries = await userSummaryLookup.GetSummariesByIdsAsync(assignedUserIds, cancellationToken);
                foreach (var item in items)
                {
                    if (!item.AssignedAgentId.HasValue) continue;
                    var key = item.AssignedAgentId.Value.ToString();
                    if (!summaries.TryGetValue(key, out var summary)) continue;
                    item.AssignedAgentName = summary.DisplayName;
                }
            }
        }

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



