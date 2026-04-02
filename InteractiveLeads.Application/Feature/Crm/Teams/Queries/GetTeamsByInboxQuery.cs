using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Teams.Queries;

public sealed class GetTeamsByInboxQuery : IApplicationRequest<IResponse>
{
    public Guid InboxId { get; set; }
}

public sealed class GetTeamsByInboxQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<GetTeamsByInboxQuery, IResponse>
{
    public async Task<IResponse> Handle(GetTeamsByInboxQuery request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);
        await ChatContext.EnsureInboxAccessAsync(db, currentUserService, request.InboxId, companyId, cancellationToken);

        var items = await db.InboxTeams
            .AsNoTracking()
            .Where(l => l.InboxId == request.InboxId)
            .OrderBy(l => l.Priority)
            .Join(db.Teams.AsNoTracking(),
                l => l.TeamId,
                t => t.Id,
                (l, t) => new { Link = l, Team = t })
            .Where(x => x.Team.CompanyId == companyId)
            .Select(x => new TeamDto
            {
                Id = x.Team.Id,
                CompanyId = x.Team.CompanyId,
                TenantId = x.Team.TenantId,
                Name = x.Team.Name,
                Description = x.Team.Description,
                IsActive = x.Team.IsActive,
                CreatedAt = x.Team.CreatedAt,
                CalendarId = x.Team.CalendarId,
                SlaPolicyId = x.Team.SlaPolicyId,
                RoutingPriority = x.Link.Priority,
                MemberCount = db.UserTeams.Count(ut => ut.TeamId == x.Team.Id),
                AutoAssignEnabled = x.Team.AutoAssignEnabled,
                AutoAssignStrategy = x.Team.AutoAssignStrategy,
                AutoAssignIgnoreOfflineUsers = x.Team.AutoAssignIgnoreOfflineUsers,
                AutoAssignMaxConversationsPerUser = x.Team.AutoAssignMaxConversationsPerUser,
                AutoAssignReassignTimeoutMinutes = x.Team.AutoAssignReassignTimeoutMinutes,
                AutoReassignOnFirstResponseSlaExpired = x.Team.AutoReassignOnFirstResponseSlaExpired
            })
            .ToListAsync(cancellationToken);

        return new ListResponse<TeamDto>(items, items.Count);
    }
}
