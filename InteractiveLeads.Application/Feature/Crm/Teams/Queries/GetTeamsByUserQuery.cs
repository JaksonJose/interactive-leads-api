using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Teams.Queries;

public sealed class GetTeamsByUserQuery : IApplicationRequest<IResponse>
{
    /// <summary>When null, uses the current user. Owners/Managers may query another user in the tenant.</summary>
    public string? UserId { get; set; }
}

public sealed class GetTeamsByUserQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<GetTeamsByUserQuery, IResponse>
{
    public async Task<IResponse> Handle(GetTeamsByUserQuery request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var targetUserId = string.IsNullOrWhiteSpace(request.UserId)
            ? currentUserService.GetUserId()
            : request.UserId!.Trim();

        var isPrivileged = currentUserService.IsInRole("Owner") || currentUserService.IsInRole("Manager");
        if (!isPrivileged && !string.Equals(targetUserId, currentUserService.GetUserId(), StringComparison.Ordinal))
        {
            var fr = new ResultResponse();
            fr.AddErrorMessage("You may only list your own teams.", "general.access_denied");
            throw new ForbiddenException(fr);
        }

        var items = await db.UserTeams
            .AsNoTracking()
            .Where(m => m.UserId == targetUserId && db.Teams.Any(t =>
                t.Id == m.TeamId && t.CompanyId == companyId && t.IsActive))
            .Join(db.Teams.AsNoTracking(),
                m => m.TeamId,
                t => t.Id,
                (m, t) => t)
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
