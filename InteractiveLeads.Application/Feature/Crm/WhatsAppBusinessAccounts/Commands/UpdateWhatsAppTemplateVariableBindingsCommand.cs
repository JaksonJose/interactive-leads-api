using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Crm;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.Commands;

public sealed class UpdateWhatsAppTemplateVariableBindingsCommand : IApplicationRequest<IResponse>
{
    public Guid WhatsAppBusinessAccountId { get; set; }

    public Guid TemplateId { get; set; }

    public UpdateWhatsAppTemplateVariableBindingsRequest Request { get; set; } = new();
}

public sealed class UpdateWhatsAppTemplateVariableBindingsRequest
{
    public WhatsAppTemplateVariableBindingDto[] VariableBindings { get; set; } = [];
}

public sealed class UpdateWhatsAppTemplateVariableBindingsCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService)
    : IApplicationRequestHandler<UpdateWhatsAppTemplateVariableBindingsCommand, IResponse>
{
    public async Task<IResponse> Handle(UpdateWhatsAppTemplateVariableBindingsCommand request, CancellationToken cancellationToken)
    {
        var companyId = await CrmCompanyResolver.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var wabaOk = await db.WhatsAppBusinessAccounts
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

        if (entity == null)
        {
            var notFound = new ResultResponse();
            notFound.AddErrorMessage("Template not found.", "general.not_found");
            throw new NotFoundException(notFound);
        }

        var rawBindings = request.Request?.VariableBindings ?? [];
        var detail = new WhatsAppTemplateDetailDto
        {
            Id = entity.Id,
            MetaTemplateId = entity.MetaTemplateId,
            LastSyncedAt = entity.LastSyncedAt,
            Name = entity.Name,
            Language = entity.Language,
            Category = entity.Category,
            Status = entity.Status,
            Body = string.Empty
        };

        WhatsAppTemplateDetailContentMapper.HydrateFromComponentsJson(detail, entity.ComponentsJson);

        var validation = WhatsAppTemplateVariableBindingsValidator.Validate(detail, rawBindings);
        if (validation != null)
            throw new BadRequestException(validation);

        var normalized = WhatsAppTemplateVariableBindingsValidator.Normalize(rawBindings);
        var mergeError = WhatsAppTemplateVariableBindingsJsonMerger.TryMerge(
            entity.ComponentsJson ?? "{}",
            normalized,
            out var newJson);

        if (mergeError != null)
            throw new BadRequestException(mergeError);

        entity.ComponentsJson = newJson;
        await db.SaveChangesAsync(cancellationToken);

        var dto = new WhatsAppTemplateDetailDto
        {
            Id = entity.Id,
            IsDisabled = entity.IsDisabled,
            DisabledAt = entity.DisabledAt,
            DisabledReason = entity.DisabledReason,
            DeletePending = entity.DeletePending,
            DeleteRequestedAt = entity.DeleteRequestedAt,
            DeleteLastError = entity.DeleteLastError,
            DeleteLastErrorCode = entity.DeleteLastErrorCode,
            DeleteLastErrorAt = entity.DeleteLastErrorAt,
            MetaTemplateId = entity.MetaTemplateId,
            LastSyncedAt = entity.LastSyncedAt,
            Name = entity.Name,
            Language = entity.Language,
            Category = entity.Category,
            Status = entity.Status,
            Body = string.Empty
        };

        WhatsAppTemplateDetailContentMapper.HydrateFromComponentsJson(dto, entity.ComponentsJson);
        dto.SubmissionLastError = entity.SubmissionLastError;
        dto.SubmissionLastErrorCode = entity.SubmissionLastErrorCode;
        dto.SubmissionLastErrorAt = entity.SubmissionLastErrorAt;
        dto.IsAvailableForMessaging = WhatsAppTemplateMessagingRules.IsAvailableForMessaging(
            entity.IsDisabled,
            entity.MetaTemplateId,
            entity.SubmissionLastErrorAt,
            entity.Status);

        return new SingleResponse<WhatsAppTemplateDetailDto>(dto);
    }
}
