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

