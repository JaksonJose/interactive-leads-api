using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Integrations.WhatsApp;

public sealed class WhatsAppBusinessAccountLinker(IApplicationDbContext db) : IWhatsAppBusinessAccountLinker
{
    public async Task<Guid?> EnsureWabaIdAsync(Guid companyId, string? metaBusinessAccountId, CancellationToken cancellationToken)
    {
        var trimmed = (metaBusinessAccountId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        var existingId = await db.WhatsAppBusinessAccounts
            .AsNoTracking()
            .Where(w => w.CompanyId == companyId && w.WabaId == trimmed)
            .Select(w => (Guid?)w.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingId.HasValue)
            return existingId;

        var entity = new WhatsAppBusinessAccount
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            WabaId = trimmed,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.WhatsAppBusinessAccounts.Add(entity);
        return entity.Id;
    }
}
