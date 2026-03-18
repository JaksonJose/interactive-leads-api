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
        if (userIds.Count == 0)
            return new Dictionary<string, (string? DisplayName, string? Email)>();

        var users = await db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id.ToString()))
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

