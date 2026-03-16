using InteractiveLeads.Application.Feature.Activation;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Infrastructure.Context.Tenancy;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Identity.Activation
{
    /// <summary>
    /// Global lookup for activation tokens in the host DB (TenantDbContext).
    /// Allows resolving token -> TenantId when the request has no tenant context.
    /// </summary>
    public class ActivationTokenLookupRepository : IActivationTokenLookupRepository
    {
        private readonly TenantDbContext _context;

        public ActivationTokenLookupRepository(TenantDbContext context)
        {
            _context = context;
        }

        public async Task<ActivationTokenLookupModel?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            var entity = await _context.ActivationTokenLookups
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
            return entity == null ? null : Map(entity);
        }

        public async Task AddAsync(string token, string tenantId, Guid userId, DateTime expiresAt, CancellationToken cancellationToken = default)
        {
            var entity = new ActivationTokenLookup
            {
                Id = Guid.NewGuid(),
                Token = token,
                TenantId = tenantId,
                UserId = userId,
                ExpiresAt = expiresAt,
                Used = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.ActivationTokenLookups.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkAsUsedAsync(string token, CancellationToken cancellationToken = default)
        {
            var entity = await _context.ActivationTokenLookups.FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
            if (entity != null)
            {
                entity.Used = true;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task InvalidateForUserAsync(string tenantId, Guid userId, CancellationToken cancellationToken = default)
        {
            var entries = await _context.ActivationTokenLookups
                .Where(t => t.TenantId == tenantId && t.UserId == userId && !t.Used)
                .ToListAsync(cancellationToken);
            foreach (var e in entries)
                e.Used = true;
            if (entries.Count > 0)
                await _context.SaveChangesAsync(cancellationToken);
        }

        private static ActivationTokenLookupModel Map(ActivationTokenLookup e)
        {
            return new ActivationTokenLookupModel
            {
                Id = e.Id,
                Token = e.Token,
                TenantId = e.TenantId,
                UserId = e.UserId,
                ExpiresAt = e.ExpiresAt,
                Used = e.Used
            };
        }
    }
}
