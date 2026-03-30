using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Feature.Crm;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.Queries;

public sealed class ListWhatsAppBusinessAccountsQuery : IApplicationRequest<IResponse>
{
}

public sealed class ListWhatsAppBusinessAccountsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    : IApplicationRequestHandler<ListWhatsAppBusinessAccountsQuery, IResponse>
{
    public async Task<IResponse> Handle(ListWhatsAppBusinessAccountsQuery request, CancellationToken cancellationToken)
    {
        var companyId = await CrmCompanyResolver.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var counts = await db.Integrations
            .AsNoTracking()
            .Where(i =>
                i.CompanyId == companyId &&
                i.Type == IntegrationType.WhatsApp &&
                i.WhatsAppBusinessAccountId != null)
            .GroupBy(i => i.WhatsAppBusinessAccountId!.Value)
            .Select(g => new { WabaPk = g.Key, Cnt = g.Count() })
            .ToDictionaryAsync(x => x.WabaPk, x => x.Cnt, cancellationToken);

        var wabas = await db.WhatsAppBusinessAccounts
            .AsNoTracking()
            .Where(w => w.CompanyId == companyId)
            .OrderBy(w => w.Name ?? w.WabaId)
            .ThenBy(w => w.WabaId)
            .ToListAsync(cancellationToken);

        var items = wabas
            .Select(w => new WhatsAppBusinessAccountListItemDto
            {
                Id = w.Id,
                WabaId = w.WabaId,
                Name = w.Name,
                IntegrationCount = counts.GetValueOrDefault(w.Id, 0)
            })
            .ToList();

        return new ListResponse<WhatsAppBusinessAccountListItemDto>(items);
    }
}
