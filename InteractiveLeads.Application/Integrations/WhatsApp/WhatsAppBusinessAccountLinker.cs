using InteractiveLeads.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Integrations.WhatsApp;

public sealed class WhatsAppBusinessAccountLinker(IApplicationDbContext db) : IWhatsAppBusinessAccountLinker
{
    /// <summary>Resolves the CRM WABA row for this company and Meta WABA id. Does not create rows — accounts must be registered first.</summary>
    public async Task<Guid?> EnsureWabaIdAsync(Guid companyId, string? metaBusinessAccountId, CancellationToken cancellationToken)
    {
        var trimmed = (metaBusinessAccountId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        return await db.WhatsAppBusinessAccounts
            .AsNoTracking()
            .Where(w => w.CompanyId == companyId && w.WabaId == trimmed)
            .Select(w => (Guid?)w.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
