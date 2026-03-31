using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Crm;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.Queries;

public sealed class GetWhatsAppTemplateQuery : IApplicationRequest<IResponse>
{
    public Guid WhatsAppBusinessAccountId { get; set; }

    public Guid TemplateId { get; set; }
}

public sealed class GetWhatsAppTemplateQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService)
    : IApplicationRequestHandler<GetWhatsAppTemplateQuery, IResponse>
{
    public async Task<IResponse> Handle(GetWhatsAppTemplateQuery request, CancellationToken cancellationToken)
    {
        var companyId = await CrmCompanyResolver.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var wabaOk = await db.WhatsAppBusinessAccounts
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WhatsAppBusinessAccountId && w.CompanyId == companyId, cancellationToken);

        if (!wabaOk)
        {
            var r = new ResultResponse();
            r.AddErrorMessage("WhatsApp Business Account not found.", "general.not_found");
            throw new NotFoundException(r);
        }

        var entity = await db.WhatsAppTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == request.TemplateId && t.WhatsAppBusinessAccountId == request.WhatsAppBusinessAccountId,
                cancellationToken);

        if (entity == null)
        {
            var r = new ResultResponse();
            r.AddErrorMessage("Template not found.", "general.not_found");
            throw new NotFoundException(r);
        }

        var dto = new WhatsAppTemplateDetailDto
        {
            Id = entity.Id,
            MetaTemplateId = entity.MetaTemplateId,
            LastSyncedAt = entity.LastSyncedAt,
            Name = entity.Name,
            Language = entity.Language,
            Category = entity.Category,
            Status = entity.Status,
            Body = string.Empty,
            SubmissionLastError = entity.SubmissionLastError,
            SubmissionLastErrorCode = entity.SubmissionLastErrorCode,
            SubmissionLastErrorAt = entity.SubmissionLastErrorAt,
            IsAvailableForMessaging = WhatsAppTemplateMessagingRules.IsAvailableForMessaging(
                entity.MetaTemplateId,
                entity.SubmissionLastErrorAt,
                entity.Status)
        };

        WhatsAppTemplateDetailContentMapper.HydrateFromComponentsJson(dto, entity.ComponentsJson);

        return new SingleResponse<WhatsAppTemplateDetailDto>(dto);
    }
}
