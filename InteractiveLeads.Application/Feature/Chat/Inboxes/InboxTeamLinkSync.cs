using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Inboxes;

/// <summary>
/// Replaces all team links for an inbox. <see cref="TeamIds"/> order defines routing priority (1 = first).
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
            .ToList();

        if (ids.Count == 0)
        {
            var empty = new ResultResponse();
            empty.AddErrorMessage("At least one team is required for this inbox.", "chat.inbox.teams_required");
            throw new BadRequestException(empty);
        }

        if (ids.Count != ids.Distinct().Count())
        {
            var dup = new ResultResponse();
            dup.AddErrorMessage("Duplicate team ids are not allowed.", "chat.inbox.teams_duplicate");
            throw new BadRequestException(dup);
        }

        var existingRows = await db.InboxTeams
            .Where(x => x.InboxId == inboxId)
            .ToListAsync(cancellationToken);

        db.InboxTeams.RemoveRange(existingRows);

        for (var i = 0; i < ids.Count; i++)
        {
            var teamId = ids[i];
            var valid = await db.Teams
                .AsNoTracking()
                .AnyAsync(t => t.Id == teamId && t.CompanyId == companyId && t.IsActive, cancellationToken);

            if (!valid)
            {
                var bad = new ResultResponse();
                bad.AddErrorMessage(
                    "One or more teams are invalid, inactive, or not in this company.",
                    "teams.bulk_link_invalid");
                throw new BadRequestException(bad);
            }

            db.InboxTeams.Add(new InboxTeam
            {
                Id = Guid.NewGuid(),
                InboxId = inboxId,
                TeamId = teamId,
                Priority = i + 1
            });
        }
    }
}
