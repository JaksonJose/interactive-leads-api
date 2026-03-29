using System.Text.Json;
using System.Text.Json.Serialization;
using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Crm;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.Commands;

public sealed class UpdateWhatsAppTemplateCommand : IApplicationRequest<IResponse>
{
    public Guid WhatsAppBusinessAccountId { get; set; }

    public Guid TemplateId { get; set; }

    public UpdateWhatsAppTemplateRequest Template { get; set; } = new();
}

public sealed class UpdateWhatsAppTemplateCommandHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    : IApplicationRequestHandler<UpdateWhatsAppTemplateCommand, IResponse>
{
    public async Task<IResponse> Handle(UpdateWhatsAppTemplateCommand request, CancellationToken cancellationToken)
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
            .Where(t => t.Id == request.TemplateId && t.WhatsAppBusinessAccountId == request.WhatsAppBusinessAccountId)
            .SingleOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            var notFound = new ResultResponse();
            notFound.AddErrorMessage("Template not found.", "general.not_found");
            throw new NotFoundException(notFound);
        }

        var u = request.Template;
        var category = (u.Category ?? string.Empty).Trim().ToUpperInvariant();
        var body = (u.Body ?? string.Empty).Trim();
        var headerText = string.IsNullOrWhiteSpace(u.HeaderText) ? null : u.HeaderText.Trim();
        var footer = string.IsNullOrWhiteSpace(u.Footer) ? null : u.Footer.Trim();

        var fieldErrors = WhatsAppTemplateMutationValidation.ValidateEditableFields(
            category,
            body,
            headerText,
            footer,
            u.Buttons);
        if (fieldErrors != null)
            throw new BadRequestException(fieldErrors);

        var merged = new CreateWhatsAppTemplateRequest
        {
            Name = entity.Name,
            Language = entity.Language,
            Category = category,
            HeaderText = headerText,
            HeaderExample = string.IsNullOrWhiteSpace(u.HeaderExample) ? null : u.HeaderExample.Trim(),
            Body = body,
            BodyExamples = u.BodyExamples,
            Footer = footer,
            Buttons = u.Buttons
        };

        entity.Category = category;
        entity.ComponentsJson = JsonSerializer.Serialize(merged, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        entity.LastSyncedAt = DateTimeOffset.UtcNow;
        entity.Status = "PENDING";

        await db.SaveChangesAsync(cancellationToken);

        var data = new CreateWhatsAppTemplateAcceptedDto
        {
            CorrelationId = entity.Id.ToString(),
            Message = "Template updated locally; Meta Graph sync is not wired yet."
        };

        return new SingleResponse<CreateWhatsAppTemplateAcceptedDto>(data);
    }
}
