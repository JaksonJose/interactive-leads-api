using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Crm;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.Queries;

public sealed class ListWhatsAppTemplatesQuery : IApplicationRequest<IResponse>
{
    public Guid WhatsAppBusinessAccountId { get; set; }
}

public sealed class ListWhatsAppTemplatesQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<ListWhatsAppTemplatesQuery, IResponse>
{
    public async Task<IResponse> Handle(ListWhatsAppTemplatesQuery request, CancellationToken cancellationToken)
    {
        var companyId = await CrmCompanyResolver.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var wabaExists = await db.WhatsAppBusinessAccounts
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WhatsAppBusinessAccountId && w.CompanyId == companyId, cancellationToken);

        if (!wabaExists)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("WhatsApp Business Account not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        var items = await db.WhatsAppTemplates
            .AsNoTracking()
            .Where(t => t.WhatsAppBusinessAccountId == request.WhatsAppBusinessAccountId)
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Language)
            .Select(t => new WhatsAppTemplateListItemDto
            {
                Id = t.Id,
                MetaTemplateId = t.MetaTemplateId,
                Name = t.Name,
                Language = t.Language,
                Category = t.Category,
                Status = t.Status,
                LastSyncedAt = t.LastSyncedAt,
                SubmissionCorrelationId = null
            })
            .ToListAsync(cancellationToken);

        return new ListResponse<WhatsAppTemplateListItemDto>(items);
    }
}
