using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Infrastructure.Context.Application;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Identity.Users;

public sealed class UserSummaryLookupService(ApplicationDbContext db) : IUserSummaryLookupService
{
    public async Task<IReadOnlyDictionary<string, (string? DisplayName, string? Email)>> GetSummariesByIdsAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken ct = default)
    {
        if (userIds is null || userIds.Count == 0)
            return new Dictionary<string, (string? DisplayName, string? Email)>();

        var guidIds = new List<Guid>(userIds.Count);
        foreach (var s in userIds)
        {
            if (!string.IsNullOrWhiteSpace(s) && Guid.TryParse(s, out var id))
                guidIds.Add(id);
        }

        if (guidIds.Count == 0)
            return new Dictionary<string, (string? DisplayName, string? Email)>();

        // Ignore Finbuckle tenant filters: they can throw when tenant context is missing in some scopes,
        // and each ApplicationDbContext instance already targets one tenant database.
        var users = await db.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(u => guidIds.Contains(u.Id))
            .Select(u => new
            {
                Id = u.Id.ToString(),
                u.FirstName,
                u.LastName,
                u.UserName,
                u.Email
            })
            .ToListAsync(ct);

        return users.ToDictionary(
            u => u.Id,
            u =>
            {
                var name = $"{u.FirstName ?? ""} {u.LastName ?? ""}".Trim();
                var display = !string.IsNullOrWhiteSpace(name) ? name : (u.UserName ?? u.Email);
                return ((string?)display, (string?)u.Email);
            });
    }
}

