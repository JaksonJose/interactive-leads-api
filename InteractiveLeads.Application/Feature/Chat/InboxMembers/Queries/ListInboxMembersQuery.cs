using System.Security.Cryptography;
using System.Text;
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

        var rows = await (
                from link in db.InboxTeams.AsNoTracking()
                join team in db.Teams.AsNoTracking() on link.TeamId equals team.Id
                where link.InboxId == request.InboxId && team.CompanyId == companyId && team.IsActive
                join ut in db.UserTeams.AsNoTracking() on team.Id equals ut.TeamId
                select new { ut.UserId, ut.JoinedAt })
            .ToListAsync(cancellationToken);

        var items = rows
            .GroupBy(x => x.UserId)
            .Select(g => new InboxMemberDto
            {
                Id = StableSyntheticMemberId(g.Key),
                InboxId = request.InboxId,
                UserId = g.Key,
                Role = null,
                IsActive = true,
                CanBeAssigned = true,
                JoinedAt = g.Min(x => x.JoinedAt)
            })
            .OrderBy(d => d.UserId, StringComparer.OrdinalIgnoreCase)
            .ToList();

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

            items.Sort((a, b) =>
                string.Compare(a.UserDisplayName ?? a.UserId, b.UserDisplayName ?? b.UserId, StringComparison.OrdinalIgnoreCase));
        }

        return new ListResponse<InboxMemberDto>(items, items.Count);
    }

    private static Guid StableSyntheticMemberId(string userId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("inbox-team-members:" + userId));
        Span<byte> slice = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(slice);
        return new Guid(slice);
    }
}
