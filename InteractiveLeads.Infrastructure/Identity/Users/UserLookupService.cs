using InteractiveLeads.Application.Feature.Identity.Impersonation;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Infrastructure.Context.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Identity.Users
{
    /// <summary>
    /// Looks up user by id without tenant filter (for impersonation).
    /// </summary>
    public class UserLookupService : IUserLookupService
    {
        private readonly ApplicationDbContext _context;

        public UserLookupService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserLookupResult?> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null)
                return null;

            var roleIds = await _context.UserRoles
                .IgnoreQueryFilters()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync(ct);
            var roleNames = await _context.Roles
                .IgnoreQueryFilters()
                .Where(r => roleIds.Contains(r.Id))
                .Select(r => r.Name!)
                .ToListAsync(ct);

            return new UserLookupResult
            {
                Id = user.Id,
                TenantId = user.TenantId,
                Email = user.Email,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Roles = roleNames
            };
        }
    }
}
