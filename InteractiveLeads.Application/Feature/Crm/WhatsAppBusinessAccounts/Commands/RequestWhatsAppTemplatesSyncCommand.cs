using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Crm;
using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.TemplateQueue;
using InteractiveLeads.Application.Integrations.Settings;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.Commands;

public sealed class RequestWhatsAppTemplatesSyncCommand : IApplicationRequest<IResponse>
{
    public Guid WhatsAppBusinessAccountId { get; set; }
}

public sealed class RequestWhatsAppTemplatesSyncCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IIntegrationSettingsResolver settingsResolver,
    ITemplateOutboundPublisher templatePublisher)
    : IApplicationRequestHandler<RequestWhatsAppTemplatesSyncCommand, IResponse>
{
    public async Task<IResponse> Handle(RequestWhatsAppTemplatesSyncCommand request, CancellationToken cancellationToken)
    {
        var companyId = await CrmCompanyResolver.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var wabaRow = await db.WhatsAppBusinessAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(w => w.Id == request.WhatsAppBusinessAccountId && w.CompanyId == companyId, cancellationToken);

        if (wabaRow is null)
        {
            var notFound = new ResultResponse();
            notFound.AddErrorMessage("WhatsApp Business Account not found.", "general.not_found");
            throw new NotFoundException(notFound);
        }

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

        var correlationId = Guid.NewGuid().ToString("D");

        var outbound = new TemplateSyncedOutboundMessage
        {
            Identifications = new TemplateSyncedOutboundIdentifications
            {
                TenantId = tenantId.ToString("N"),
                WabaId = wabaRow.WabaId.Trim(),
                IntegrationId = integration.Id.ToString("D"),
                WhatsAppBusinessAccountId = request.WhatsAppBusinessAccountId.ToString("D"),
                CorrelationId = correlationId
            },
            Auth = new TemplateCreateOutboundAuth
            {
                AccessToken = ws.AccessToken.Trim(),
                PhoneNumberId = ws.PhoneNumberId.Trim(),
                BusinessAccountId = businessAccountId
            },
            Payload = new TemplateSyncedOutboundPayload { SyncAll = true }
        };

        await templatePublisher.PublishTemplateSyncedAsync(outbound, cancellationToken);

        var data = new CreateWhatsAppTemplateAcceptedDto
        {
            CorrelationId = correlationId,
            Message = templatePublisher.PublishesToBroker
                ? "Template sync queued for WhatsApp (Meta) via n8n."
                : "Sync was not sent to the broker. Enable RabbitMQ to publish template_synced."
        };

        return new SingleResponse<CreateWhatsAppTemplateAcceptedDto>(data);
    }
}
