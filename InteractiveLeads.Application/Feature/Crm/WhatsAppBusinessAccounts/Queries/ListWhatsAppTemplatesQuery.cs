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

        // Optional text search (q): matches both template name and serialized components (header/body/footer).
        var q = (pagination.Filters ?? [])
            .FirstOrDefault(f => string.Equals(f.Field?.Trim(), "q", StringComparison.OrdinalIgnoreCase))
            ?.Value
            ?.Trim();

        if (!string.IsNullOrWhiteSpace(q))
        {
            // ComponentsJson is stored as jsonb; use ::text to enable text search.
            // Using raw SQL here avoids generating invalid SQL like lower(jsonb).
            var like = $"%{q}%";
            baseQuery = db.WhatsAppTemplates
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM "Crm"."WhatsAppTemplate" t
                    WHERE t."WhatsAppBusinessAccountId" = {request.WhatsAppBusinessAccountId}
                      AND (
                        t."Name" ILIKE {like}
                        OR t."ComponentsJson"::text ILIKE {like}
                      )
                    """)
                .AsNoTracking();
        }

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

        var rows = await ordered
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .Select(t => new
            {
                t.Id,
                t.IsDisabled,
                t.DisabledAt,
                t.DisabledReason,
                t.DeletePending,
                t.DeleteRequestedAt,
                t.DeleteLastError,
                t.DeleteLastErrorCode,
                t.DeleteLastErrorAt,
                t.MetaTemplateId,
                t.Name,
                t.Language,
                t.Category,
                t.Status,
                t.LastSyncedAt,
                t.ComponentsJson,
                t.SubmissionLastError,
                t.SubmissionLastErrorCode,
                t.SubmissionLastErrorAt
            })
            .ToListAsync(cancellationToken);

        var items = new List<WhatsAppTemplateListItemDto>(rows.Count);
        foreach (var r in rows)
        {
            var detail = new WhatsAppTemplateDetailDto();
            WhatsAppTemplateDetailContentMapper.HydrateFromComponentsJson(detail, r.ComponentsJson ?? "{}");
            items.Add(new WhatsAppTemplateListItemDto
            {
                Id = r.Id,
                IsDisabled = r.IsDisabled,
                DisabledAt = r.DisabledAt,
                DisabledReason = r.DisabledReason,
                DeletePending = r.DeletePending,
                DeleteRequestedAt = r.DeleteRequestedAt,
                DeleteLastError = r.DeleteLastError,
                DeleteLastErrorCode = r.DeleteLastErrorCode,
                DeleteLastErrorAt = r.DeleteLastErrorAt,
                MetaTemplateId = r.MetaTemplateId,
                Name = r.Name,
                Language = r.Language,
                Category = r.Category,
                Status = r.Status,
                LastSyncedAt = r.LastSyncedAt,
                SubmissionCorrelationId = null,
                VariableSlotCount = detail.VariableSlotCount,
                VariableBindingsComplete = detail.VariableBindingsComplete,
                SubmissionLastError = r.SubmissionLastError,
                SubmissionLastErrorCode = r.SubmissionLastErrorCode,
                SubmissionLastErrorAt = r.SubmissionLastErrorAt,
                IsAvailableForMessaging = WhatsAppTemplateMessagingRules.IsAvailableForMessaging(
                    r.IsDisabled,
                    r.MetaTemplateId,
                    r.SubmissionLastErrorAt,
                    r.Status)
            });
        }

        return new ListResponse<WhatsAppTemplateListItemDto>(items, total);
    }
}
