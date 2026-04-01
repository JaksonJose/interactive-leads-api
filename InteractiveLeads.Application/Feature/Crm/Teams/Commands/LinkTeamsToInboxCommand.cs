using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Teams.Commands;

public sealed class LinkTeamsToInboxCommand : IApplicationRequest<IResponse>
{
    public Guid InboxId { get; set; }
    public LinkTeamsToInboxRequest Body { get; set; } = new();
}

public sealed class LinkTeamsToInboxCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<LinkTeamsToInboxCommand, IResponse>
{
    public async Task<IResponse> Handle(LinkTeamsToInboxCommand request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);
        await ChatContext.EnsureInboxAccessAsync(db, currentUserService, request.InboxId, companyId, cancellationToken);

        var ids = (request.Body?.TeamIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return new ResultResponse();

        var validTeams = await db.Teams
            .AsNoTracking()
            .Where(t => ids.Contains(t.Id) && t.CompanyId == companyId && t.IsActive)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        if (validTeams.Count != ids.Count)
        {
            var bad = new ResultResponse();
            bad.AddErrorMessage("One or more teams are invalid, inactive, or not in this company.", "teams.bulk_link_invalid");
            throw new BadRequestException(bad);
        }

        var existing = await db.InboxTeams
            .Where(x => x.InboxId == request.InboxId && ids.Contains(x.TeamId))
            .Select(x => x.TeamId)
            .ToListAsync(cancellationToken);

        var existingSet = existing.ToHashSet();
        foreach (var teamId in validTeams)
        {
            if (existingSet.Contains(teamId))
                continue;
            db.InboxTeams.Add(new InboxTeam
            {
                Id = Guid.NewGuid(),
                InboxId = request.InboxId,
                TeamId = teamId
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return new ResultResponse();
    }
}
