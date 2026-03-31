using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Crm;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.Commands;

public sealed class SetWhatsAppTemplateDisabledCommand : IApplicationRequest<IResponse>
{
    public Guid WhatsAppBusinessAccountId { get; set; }

    public Guid TemplateId { get; set; }

    public bool IsDisabled { get; set; }
}

public sealed class SetWhatsAppTemplateDisabledCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService)
    : IApplicationRequestHandler<SetWhatsAppTemplateDisabledCommand, IResponse>
{
    public async Task<IResponse> Handle(SetWhatsAppTemplateDisabledCommand request, CancellationToken cancellationToken)
    {
        var companyId = await CrmCompanyResolver.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var wabaOk = await db.WhatsAppBusinessAccounts
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WhatsAppBusinessAccountId && w.CompanyId == companyId, cancellationToken);

        if (!wabaOk)
        {
            var notFound = new ResultResponse();
            notFound.AddErrorMessage("WhatsApp Business Account not found.", "general.not_found");
            throw new NotFoundException(notFound);
        }

        var entity = await db.WhatsAppTemplates
            .FirstOrDefaultAsync(
                t => t.Id == request.TemplateId && t.WhatsAppBusinessAccountId == request.WhatsAppBusinessAccountId,
                cancellationToken);

        if (entity is null)
        {
            var notFound = new ResultResponse();
            notFound.AddErrorMessage("Template not found.", "general.not_found");
            throw new NotFoundException(notFound);
        }

        if (entity.DeletePending && request.IsDisabled == false)
        {
            var bad = new ResultResponse();
            bad.AddErrorMessage(
                "Template is pending delete; it cannot be re-enabled until the delete completes or fails.",
                "integrations.templates.enable_blocked_delete_pending");
            throw new BadRequestException(bad);
        }

        entity.IsDisabled = request.IsDisabled;
        entity.DisabledAt = request.IsDisabled ? DateTimeOffset.UtcNow : null;
        entity.DisabledReason = request.IsDisabled ? "user_disabled" : null;
        await db.SaveChangesAsync(cancellationToken);

        return new SingleResponse<object>(new
        {
            entity.Id,
            entity.IsDisabled,
            entity.DisabledAt,
            entity.DisabledReason
        });
    }
}

