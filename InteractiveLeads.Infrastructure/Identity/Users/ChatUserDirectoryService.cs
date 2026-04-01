using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Infrastructure.Context.Application;
using InteractiveLeads.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Identity.Users;

public sealed class ChatUserDirectoryService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUserService) : IChatUserDirectoryService
{
    public async Task<IReadOnlyList<ChatDirectoryUserRow>> ListAsync(
        ChatDirectoryMode mode,
        Guid? inboxId,
        Guid? teamId,
        CancellationToken cancellationToken)
    {
        var tenantId = currentUserService.GetUserTenant();
        if (string.IsNullOrWhiteSpace(tenantId))
            return [];

        IQueryable<ApplicationUser> usersQuery = db.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId && u.IsActive);

        if (teamId.HasValue && teamId.Value != Guid.Empty)
        {
            var crmTenantId = await db.Tenants
                .AsNoTracking()
                .Where(t => t.Identifier == tenantId)
                .Select(t => t.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (crmTenantId == Guid.Empty)
                return [];

            var team = await db.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                        t.Id == teamId.Value && t.TenantId == crmTenantId && t.IsActive,
                    cancellationToken);

            if (team is null)
                return [];

            var teamUserIds = await db.UserTeams
                .AsNoTracking()
                .Where(m => m.TeamId == teamId.Value)
                .Select(m => m.UserId)
                .ToListAsync(cancellationToken);

            if (teamUserIds.Count == 0)
                return [];

            usersQuery = usersQuery.Where(u => teamUserIds.Contains(u.Id.ToString()));
        }

        if (mode == ChatDirectoryMode.Responsible)
        {
            if (!inboxId.HasValue || inboxId.Value == Guid.Empty)
                return [];

            var memberIds = await (
                    from inbox in db.Inboxes.AsNoTracking()
                    where inbox.Id == inboxId.Value
                    join link in db.InboxTeams.AsNoTracking() on inbox.Id equals link.InboxId
                    join team in db.Teams.AsNoTracking() on link.TeamId equals team.Id
                    where team.CompanyId == inbox.CompanyId && team.IsActive
                    join ut in db.UserTeams.AsNoTracking() on team.Id equals ut.TeamId
                    select ut.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (memberIds.Count == 0)
                return [];

            usersQuery = usersQuery.Where(u => memberIds.Contains(u.Id.ToString()));
        }

        var users = await usersQuery
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.UserName, u.Email })
            .ToListAsync(cancellationToken);

        var rows = new List<ChatDirectoryUserRow>(users.Count);
        foreach (var u in users)
        {
            var appUser = await userManager.FindByIdAsync(u.Id.ToString());
            if (appUser is null)
                continue;

            var roles = await userManager.GetRolesAsync(appUser);

            if (mode == ChatDirectoryMode.Responsible && !roles.Contains("Agent"))
                continue;

            var name = $"{u.FirstName ?? ""} {u.LastName ?? ""}".Trim();
            var display = !string.IsNullOrWhiteSpace(name) ? name : (u.UserName ?? u.Email ?? u.Id.ToString());
            rows.Add(new ChatDirectoryUserRow(
                u.Id.ToString("D"),
                display,
                u.Email,
                roles.OrderBy(r => r).ToList()));
        }

        return rows;
    }
}
