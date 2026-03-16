using InteractiveLeads.Application.Feature.Activation;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Infrastructure.Context.Application;
using InteractiveLeads.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Identity.Activation
{
    /// <summary>
    /// Repository for user activation tokens. GetByTokenAsync uses IgnoreQueryFilters to resolve token across tenants (for activation endpoint).
    /// </summary>
    public class ActivationTokenRepository : IActivationTokenRepository
    {
        private readonly ApplicationDbContext _context;

        public ActivationTokenRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ActivationTokenModel?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            var entity = await _context.ActivationTokens
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
            return entity == null ? null : Map(entity);
        }

        public async Task<ActivationTokenModel> AddAsync(Guid userId, string token, DateTime expiresAt, CancellationToken cancellationToken = default)
        {
            var entity = new UserActivationToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = token,
                ExpiresAt = expiresAt,
                Used = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.ActivationTokens.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return Map(entity);
        }

        public async Task MarkAsUsedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _context.ActivationTokens.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
            if (entity != null)
            {
                entity.Used = true;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task InvalidateTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var tokens = await _context.ActivationTokens
                .Where(t => t.UserId == userId && !t.Used)
                .ToListAsync(cancellationToken);

            if (tokens.Count == 0)
            {
                return;
            }

            foreach (var token in tokens)
            {
                token.Used = true;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private static ActivationTokenModel Map(UserActivationToken e)
        {
            return new ActivationTokenModel
            {
                Id = e.Id,
                UserId = e.UserId,
                Token = e.Token,
                ExpiresAt = e.ExpiresAt,
                Used = e.Used
            };
        }
    }
}
