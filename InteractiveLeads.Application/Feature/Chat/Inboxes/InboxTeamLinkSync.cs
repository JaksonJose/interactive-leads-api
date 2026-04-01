using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Inboxes;

/// <summary>
/// Replaces all team links for an inbox (same validation rules as bulk link).
/// </summary>
public static class InboxTeamLinkSync
{
    public static async Task ReplaceLinksAsync(
        IApplicationDbContext db,
        Guid companyId,
        Guid inboxId,
        IReadOnlyList<Guid>? teamIds,
        CancellationToken cancellationToken)
    {
        var ids = (teamIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var existingRows = await db.InboxTeams
            .Where(x => x.InboxId == inboxId)
            .ToListAsync(cancellationToken);

        db.InboxTeams.RemoveRange(existingRows);

        if (ids.Count == 0)
            return;

        var validTeams = await db.Teams
            .AsNoTracking()
            .Where(t => ids.Contains(t.Id) && t.CompanyId == companyId && t.IsActive)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        if (validTeams.Count != ids.Count)
        {
            var bad = new ResultResponse();
            bad.AddErrorMessage(
                "One or more teams are invalid, inactive, or not in this company.",
                "teams.bulk_link_invalid");
            throw new BadRequestException(bad);
        }

        foreach (var teamId in validTeams)
        {
            db.InboxTeams.Add(new InboxTeam
            {
                Id = Guid.NewGuid(),
                InboxId = inboxId,
                TeamId = teamId
            });
        }
    }
}
