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
            .Join(db.Teams.AsNoTracking(),
                l => l.TeamId,
                t => t.Id,
                (l, t) => t)
            .Where(t => t.CompanyId == companyId)
            .OrderByDescending(t => t.IsActive)
            .ThenBy(t => t.Name)
            .Select(t => new TeamDto
            {
                Id = t.Id,
                CompanyId = t.CompanyId,
                TenantId = t.TenantId,
                Name = t.Name,
                Description = t.Description,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                CalendarId = t.CalendarId,
                SlaPolicyId = t.SlaPolicyId,
                MemberCount = db.UserTeams.Count(ut => ut.TeamId == t.Id)
            })
            .ToListAsync(cancellationToken);

        return new ListResponse<TeamDto>(items, items.Count);
    }
}
