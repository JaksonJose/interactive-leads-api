using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.TemplateQueue;
using InteractiveLeads.Application.Integrations.Settings;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.Commands;

public sealed class DeleteWhatsAppTemplateCommand : IApplicationRequest<IResponse>
{
    public Guid WhatsAppBusinessAccountId { get; set; }

    public Guid TemplateId { get; set; }

    public DeleteWhatsAppTemplateRequest Request { get; set; } = new();
}

public sealed class DeleteWhatsAppTemplateCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IIntegrationSettingsResolver settingsResolver,
    ITemplateOutboundPublisher templatePublisher)
    : IApplicationRequestHandler<DeleteWhatsAppTemplateCommand, IResponse>
{
    public async Task<IResponse> Handle(DeleteWhatsAppTemplateCommand request, CancellationToken cancellationToken)
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

        var submittedName = (request.Request.Name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(submittedName))
        {
            var bad = new ResultResponse();
            bad.AddErrorMessage("Template name is required.", "integrations.templates.delete_name_required");
            throw new BadRequestException(bad);
        }

        if (!string.Equals(submittedName, entity.Name.Trim(), StringComparison.Ordinal))
        {
            var bad = new ResultResponse();
            bad.AddErrorMessage("Template name does not match this template.", "integrations.templates.delete_name_mismatch");
            throw new BadRequestException(bad);
        }

        var correlationId = entity.Id.ToString("D");
        var name = entity.Name.Trim();
        var language = entity.Language.Trim();

        var wabaRow = await db.WhatsAppBusinessAccounts
            .AsNoTracking()
            .FirstAsync(w => w.Id == request.WhatsAppBusinessAccountId, cancellationToken);

        var tenantId = await db.Companies
            .Where(c => c.Id == companyId)
            .Select(c => c.TenantId)
            .FirstAsync(cancellationToken);

        var integration = await db.Integrations
            .Where(i =>
                i.CompanyId == companyId
                && i.WhatsAppBusinessAccountId == request.WhatsAppBusinessAccountId
                && i.Type == IntegrationType.WhatsApp
                && i.IsActive)
            .OrderBy(i => i.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (integration is null)
        {
            var missingIntegration = new ResultResponse();
            missingIntegration.AddErrorMessage(
                "No active WhatsApp number is linked to this Business Account. Add a phone integration first.",
                "integrations.templates.no_whatsapp_integration");
            throw new BadRequestException(missingIntegration);
        }

        var ws = (WhatsAppSettings)settingsResolver.Deserialize(IntegrationType.WhatsApp, integration.Settings);
        if (string.IsNullOrWhiteSpace(ws.AccessToken) || string.IsNullOrWhiteSpace(ws.PhoneNumberId))
        {
            var creds = new ResultResponse();
            creds.AddErrorMessage(
                "WhatsApp integration is missing access token or phone number id.",
                "integrations.templates.missing_credentials");
            throw new BadRequestException(creds);
        }

        var businessAccountId = string.IsNullOrWhiteSpace(ws.BusinessAccountId)
            ? wabaRow.WabaId
            : ws.BusinessAccountId.Trim();

        var outbound = new TemplateDeleteOutboundMessage
        {
            TenantId = tenantId.ToString("N"),
            WabaId = wabaRow.WabaId.Trim(),
            Auth = new TemplateCreateOutboundAuth
            {
                AccessToken = ws.AccessToken.Trim(),
                PhoneNumberId = ws.PhoneNumberId.Trim(),
                BusinessAccountId = businessAccountId
            },
            Payload = new TemplateDeleteOutboundPayload
            {
                Name = name,
                Language = language
            },
            Metadata = new TemplateCreateOutboundMetadata
            {
                CorrelationId = correlationId,
                IntegrationId = integration.Id.ToString("D"),
                WhatsAppBusinessAccountId = request.WhatsAppBusinessAccountId.ToString("D")
            }
        };

        await templatePublisher.PublishDeleteTemplateAsync(outbound, cancellationToken);

        db.WhatsAppTemplates.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);

        var data = new CreateWhatsAppTemplateAcceptedDto
        {
            CorrelationId = correlationId,
            Message = templatePublisher.PublishesToBroker
                ? "Template delete queued for WhatsApp (Meta)."
                : "Template removed locally. Enable RabbitMQ to publish delete to the template queue."
        };

        return new SingleResponse<CreateWhatsAppTemplateAcceptedDto>(data);
    }
}
