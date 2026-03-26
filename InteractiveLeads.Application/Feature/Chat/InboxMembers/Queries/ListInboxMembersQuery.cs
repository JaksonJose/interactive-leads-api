using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.InboxMembers.Queries;

public sealed class ListInboxMembersQuery : IApplicationRequest<IResponse>
{
    public Guid InboxId { get; set; }
}

public sealed class ListInboxMembersQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IUserSummaryLookupService userSummaryLookup) : IApplicationRequestHandler<ListInboxMembersQuery, IResponse>
{
    public async Task<IResponse> Handle(ListInboxMembersQuery request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);
        await ChatContext.EnsureInboxAccessAsync(db, currentUserService, request.InboxId, companyId, cancellationToken);

        var items = await db.InboxMembers
            .AsNoTracking()
            .Where(m => m.InboxId == request.InboxId)
            .OrderByDescending(m => m.IsActive)
            .ThenBy(m => m.UserId)
            .Select(m => new InboxMemberDto
            {
                Id = m.Id,
                InboxId = m.InboxId,
                UserId = m.UserId,
                Role = m.Role,
                IsActive = m.IsActive,
                CanBeAssigned = m.CanBeAssigned,
                JoinedAt = m.JoinedAt
            })
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
        {
            var userIds = items
                .Select(i => i.UserId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            var summaries = await userSummaryLookup.GetSummariesByIdsAsync(userIds, cancellationToken);
            foreach (var item in items)
            {
                if (!summaries.TryGetValue(item.UserId, out var summary)) continue;
                item.UserDisplayName = summary.DisplayName;
                item.UserEmail = summary.Email;
            }
        }

        return new ListResponse<InboxMemberDto>(items, items.Count);
    }
}


