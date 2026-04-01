using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Teams.Queries;

public sealed class GetInboxesByTeamQuery : IApplicationRequest<IResponse>
{
    public Guid TeamId { get; set; }
}

public sealed class GetInboxesByTeamQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<GetInboxesByTeamQuery, IResponse>
{
    public async Task<IResponse> Handle(GetInboxesByTeamQuery request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var teamOk = await db.Teams
            .AsNoTracking()
            .AnyAsync(t => t.Id == request.TeamId && t.CompanyId == companyId, cancellationToken);

        if (!teamOk)
        {
            var nf = new ResultResponse();
            nf.AddErrorMessage("Team not found.", "general.not_found");
            throw new NotFoundException(nf);
        }

        if (currentUserService.IsInRole("Agent")
            && !currentUserService.IsInRole("Owner")
            && !currentUserService.IsInRole("Manager"))
        {
            var userId = currentUserService.GetUserId();
            var onTeam = await db.UserTeams
                .AsNoTracking()
                .AnyAsync(m => m.TeamId == request.TeamId && m.UserId == userId, cancellationToken);
            if (!onTeam)
            {
                var fr = new ResultResponse();
                fr.AddErrorMessage("You are not authorized to view this team.", "general.access_denied");
                throw new ForbiddenException(fr);
            }
        }

        var items = await db.InboxTeams
            .AsNoTracking()
            .Where(l => l.TeamId == request.TeamId)
            .Join(db.Inboxes.AsNoTracking(),
                l => l.InboxId,
                i => i.Id,
                (l, i) => i)
            .Where(i => i.CompanyId == companyId)
            .OrderBy(i => i.Name)
            .Select(i => new InboxSummaryForTeamDto
            {
                Id = i.Id,
                Name = i.Name,
                IsActive = i.IsActive
            })
            .ToListAsync(cancellationToken);

        return new ListResponse<InboxSummaryForTeamDto>(items, items.Count);
    }
}
