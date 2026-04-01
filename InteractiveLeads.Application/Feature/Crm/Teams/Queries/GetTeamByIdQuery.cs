using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Teams.Queries;

public sealed class GetTeamByIdQuery : IApplicationRequest<IResponse>
{
    public Guid TeamId { get; set; }
}

public sealed class GetTeamByIdQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<GetTeamByIdQuery, IResponse>
{
    public async Task<IResponse> Handle(GetTeamByIdQuery request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var dto = await db.Teams
            .AsNoTracking()
            .Where(t => t.Id == request.TeamId && t.CompanyId == companyId)
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
                MemberCount = db.UserTeams.Count(ut => ut.TeamId == t.Id),
                AutoAssignEnabled = t.AutoAssignEnabled,
                AutoAssignStrategy = t.AutoAssignStrategy,
                AutoAssignIgnoreOfflineUsers = t.AutoAssignIgnoreOfflineUsers,
                AutoAssignMaxConversationsPerUser = t.AutoAssignMaxConversationsPerUser,
                AutoAssignReassignTimeoutMinutes = t.AutoAssignReassignTimeoutMinutes
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
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

        return new SingleResponse<TeamDto>(dto);
    }
}
