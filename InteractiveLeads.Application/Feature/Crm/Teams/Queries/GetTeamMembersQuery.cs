using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Teams.Queries;

public sealed class GetTeamMembersQuery : IApplicationRequest<IResponse>
{
    public Guid TeamId { get; set; }
}

public sealed class GetTeamMembersQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<GetTeamMembersQuery, IResponse>
{
    public async Task<IResponse> Handle(GetTeamMembersQuery request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var team = await db.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TeamId && t.CompanyId == companyId, cancellationToken);

        if (team is null)
        {
            var nf = new ResultResponse();
            nf.AddErrorMessage("Team not found.", "general.not_found");
            throw new NotFoundException(nf);
        }

        var isPrivileged = currentUserService.IsInRole("Owner") || currentUserService.IsInRole("Manager");
        if (!isPrivileged)
        {
            if (!team.IsActive)
            {
                var nf = new ResultResponse();
                nf.AddErrorMessage("Team not found.", "general.not_found");
                throw new NotFoundException(nf);
            }

            var userId = currentUserService.GetUserId();
            var isMember = await db.UserTeams
                .AnyAsync(m => m.TeamId == request.TeamId && m.UserId == userId, cancellationToken);
            if (!isMember)
            {
                var fr = new ResultResponse();
                fr.AddErrorMessage("You are not allowed to view this team.", "general.access_denied");
                throw new ForbiddenException(fr);
            }
        }

        var items = await db.UserTeams
            .AsNoTracking()
            .Where(m => m.TeamId == request.TeamId)
            .OrderBy(m => m.UserId)
            .Select(m => new TeamMemberDto
            {
                UserId = m.UserId,
                Role = m.Role,
                JoinedAt = m.JoinedAt
            })
            .ToListAsync(cancellationToken);

        return new ListResponse<TeamMemberDto>(items, items.Count);
    }
}
