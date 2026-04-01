using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Teams.Commands;

public sealed class UnlinkTeamFromInboxCommand : IApplicationRequest<IResponse>
{
    public Guid TeamId { get; set; }
    public Guid InboxId { get; set; }
}

public sealed class UnlinkTeamFromInboxCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<UnlinkTeamFromInboxCommand, IResponse>
{
    public async Task<IResponse> Handle(UnlinkTeamFromInboxCommand request, CancellationToken cancellationToken)
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

        await ChatContext.EnsureInboxAccessAsync(db, currentUserService, request.InboxId, companyId, cancellationToken);

        var link = await db.InboxTeams
            .FirstOrDefaultAsync(x => x.InboxId == request.InboxId && x.TeamId == request.TeamId, cancellationToken);

        if (link is null)
        {
            var nf = new ResultResponse();
            nf.AddErrorMessage("Link not found.", "general.not_found");
            throw new NotFoundException(nf);
        }

        var totalLinks = await db.InboxTeams
            .CountAsync(x => x.InboxId == request.InboxId, cancellationToken);

        if (totalLinks <= 1)
        {
            var bad = new ResultResponse();
            bad.AddErrorMessage("Cannot remove the last team from an inbox.", "chat.inbox.last_team");
            throw new BadRequestException(bad);
        }

        db.InboxTeams.Remove(link);
        await db.SaveChangesAsync(cancellationToken);

        var remaining = await db.InboxTeams
            .Where(x => x.InboxId == request.InboxId)
            .OrderBy(x => x.Priority)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < remaining.Count; i++)
            remaining[i].Priority = i + 1;

        await db.SaveChangesAsync(cancellationToken);

        return new ResultResponse();
    }
}
