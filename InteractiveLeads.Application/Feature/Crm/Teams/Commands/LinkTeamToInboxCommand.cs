using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Teams.Commands;

public sealed class LinkTeamToInboxCommand : IApplicationRequest<IResponse>
{
    public Guid TeamId { get; set; }
    public Guid InboxId { get; set; }
}

public sealed class LinkTeamToInboxCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<LinkTeamToInboxCommand, IResponse>
{
    public async Task<IResponse> Handle(LinkTeamToInboxCommand request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var team = await db.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TeamId && t.CompanyId == companyId && t.IsActive, cancellationToken);

        if (team is null)
        {
            var nf = new ResultResponse();
            nf.AddErrorMessage("Team not found.", "general.not_found");
            throw new NotFoundException(nf);
        }

        await ChatContext.EnsureInboxAccessAsync(db, currentUserService, request.InboxId, companyId, cancellationToken);

        var inboxCompany = await db.Inboxes
            .AsNoTracking()
            .Where(i => i.Id == request.InboxId)
            .Select(i => i.CompanyId)
            .SingleAsync(cancellationToken);

        if (inboxCompany != team.CompanyId)
        {
            var bad = new ResultResponse();
            bad.AddErrorMessage("Inbox and team must belong to the same company.", "teams.cross_company");
            throw new BadRequestException(bad);
        }

        var exists = await db.InboxTeams
            .AnyAsync(x => x.InboxId == request.InboxId && x.TeamId == request.TeamId, cancellationToken);
        if (exists)
        {
            return new ResultResponse();
        }

        var maxPriority = await db.InboxTeams
            .Where(x => x.InboxId == request.InboxId)
            .MaxAsync(x => (int?)x.Priority, cancellationToken) ?? 0;

        db.InboxTeams.Add(new InboxTeam
        {
            Id = Guid.NewGuid(),
            InboxId = request.InboxId,
            TeamId = request.TeamId,
            Priority = maxPriority + 1
        });

        await db.SaveChangesAsync(cancellationToken);
        return new ResultResponse();
    }
}
