using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Crm;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Requests;
using InteractiveLeads.Application.Requests.Enums;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.Queries;

public sealed class ListWhatsAppTemplatesQuery : IApplicationRequest<IResponse>
{
    public Guid WhatsAppBusinessAccountId { get; set; }

    /// <summary>Pagination, sort, and optional filters (same shape as tenant <see cref="InquiryRequest"/>).</summary>
    public InquiryRequest Pagination { get; set; } = new();
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

        var pagination = request.Pagination ?? new InquiryRequest();
        if (!pagination.IsValid())
            pagination = new InquiryRequest();

        var baseQuery = db.WhatsAppTemplates
            .AsNoTracking()
            .Where(t => t.WhatsAppBusinessAccountId == request.WhatsAppBusinessAccountId);

        var total = await baseQuery.CountAsync(cancellationToken);

        var field = (pagination.SortBy ?? "name").Trim().ToLowerInvariant();
        var desc = pagination.SortOrder == SortDirection.Descending;

        IOrderedQueryable<WhatsAppTemplate> ordered = field switch
        {
            "language" => desc
                ? baseQuery.OrderByDescending(t => t.Language).ThenByDescending(t => t.Name)
                : baseQuery.OrderBy(t => t.Language).ThenBy(t => t.Name),
            "category" => desc
                ? baseQuery.OrderByDescending(t => t.Category).ThenByDescending(t => t.Name)
                : baseQuery.OrderBy(t => t.Category).ThenBy(t => t.Name),
            "status" => desc
                ? baseQuery.OrderByDescending(t => t.Status).ThenByDescending(t => t.Name)
                : baseQuery.OrderBy(t => t.Status).ThenBy(t => t.Name),
            "metatemplateid" => desc
                ? baseQuery.OrderByDescending(t => t.MetaTemplateId).ThenByDescending(t => t.Name)
                : baseQuery.OrderBy(t => t.MetaTemplateId).ThenBy(t => t.Name),
            "lastsyncedat" => desc
                ? baseQuery.OrderByDescending(t => t.LastSyncedAt)
                : baseQuery.OrderBy(t => t.LastSyncedAt),
            _ => desc
                ? baseQuery.OrderByDescending(t => t.Name).ThenByDescending(t => t.Language)
                : baseQuery.OrderBy(t => t.Name).ThenBy(t => t.Language)
        };

        var items = await ordered
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
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

        return new ListResponse<WhatsAppTemplateListItemDto>(items, total);
    }
}
