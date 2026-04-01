using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Teams.Queries;

public sealed class GetTeamsByCompanyQuery : IApplicationRequest<IResponse>
{
}

public sealed class GetTeamsByCompanyQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<GetTeamsByCompanyQuery, IResponse>
{
    public async Task<IResponse> Handle(GetTeamsByCompanyQuery request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var query = db.Teams.AsNoTracking().Where(t => t.CompanyId == companyId && t.IsActive);

        if (currentUserService.IsInRole("Agent")
            && !currentUserService.IsInRole("Owner")
            && !currentUserService.IsInRole("Manager"))
        {
            var userId = currentUserService.GetUserId();
            query = query.Where(t => db.UserTeams.Any(m => m.TeamId == t.Id && m.UserId == userId));
        }

        var items = await query
            .OrderBy(t => t.Name)
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
