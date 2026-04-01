using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Teams.Commands;

public sealed class RemoveUserFromTeamCommand : IApplicationRequest<IResponse>
{
    public Guid TeamId { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public sealed class RemoveUserFromTeamCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<RemoveUserFromTeamCommand, IResponse>
{
    public async Task<IResponse> Handle(RemoveUserFromTeamCommand request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var team = await db.Teams
            .AsNoTracking()
            .AnyAsync(t => t.Id == request.TeamId && t.CompanyId == companyId, cancellationToken);

        if (!team)
        {
            var nf = new ResultResponse();
            nf.AddErrorMessage("Team not found.", "general.not_found");
            throw new NotFoundException(nf);
        }

        var userId = (request.UserId ?? string.Empty).Trim();
        var link = await db.UserTeams
            .FirstOrDefaultAsync(m => m.TeamId == request.TeamId && m.UserId == userId, cancellationToken);

        if (link is null)
        {
            var nf = new ResultResponse();
            nf.AddErrorMessage("User is not a member of this team.", "general.not_found");
            throw new NotFoundException(nf);
        }

        db.UserTeams.Remove(link);
        await db.SaveChangesAsync(cancellationToken);

        return new ResultResponse();
    }
}
