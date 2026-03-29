using System.Text.Json;
using System.Text.Json.Serialization;
using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Crm;
using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.TemplateQueue;
using InteractiveLeads.Application.Integrations.Settings;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.Commands;

public sealed class CreateWhatsAppTemplateCommand : IApplicationRequest<IResponse>
{
    public Guid WhatsAppBusinessAccountId { get; set; }

    public CreateWhatsAppTemplateRequest Template { get; set; } = new();
}

public sealed class CreateWhatsAppTemplateCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IIntegrationSettingsResolver settingsResolver,
    ITemplateOutboundPublisher templatePublisher)
    : IApplicationRequestHandler<CreateWhatsAppTemplateCommand, IResponse>
{
    public async Task<IResponse> Handle(CreateWhatsAppTemplateCommand request, CancellationToken cancellationToken)
    {
        var companyId = await CrmCompanyResolver.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var wabaExists = await db.WhatsAppBusinessAccounts
            .AnyAsync(w => w.Id == request.WhatsAppBusinessAccountId && w.CompanyId == companyId, cancellationToken);

        if (!wabaExists)
        {
            var notFound = new ResultResponse();
            notFound.AddErrorMessage("WhatsApp Business Account not found.", "general.not_found");
            throw new NotFoundException(notFound);
        }

        var t = request.Template;
        var name = (t.Name ?? string.Empty).Trim();
        var language = (t.Language ?? string.Empty).Trim();
        var category = (t.Category ?? string.Empty).Trim().ToUpperInvariant();
        var body = (t.Body ?? string.Empty).Trim();

        var badRequest = new ResultResponse();
        if (name.Length is < 1 or > 512)
            badRequest.AddErrorMessage("Template name length is invalid.", "integrations.templates.invalid_name");
        if (language.Length is < 1 or > 32)
            badRequest.AddErrorMessage("Template language is invalid.", "integrations.templates.invalid_language");

        var headerText = string.IsNullOrWhiteSpace(t.HeaderText) ? null : t.HeaderText.Trim();
        var footer = string.IsNullOrWhiteSpace(t.Footer) ? null : t.Footer.Trim();
        var fieldErrors = WhatsAppTemplateMutationValidation.ValidateEditableFields(
            category,
            body,
            headerText,
            footer,
            t.Buttons);
        if (fieldErrors != null)
            badRequest.Messages.AddRange(fieldErrors.Messages);

        if (badRequest.Messages is { Count: > 0 })
            throw new BadRequestException(badRequest);

        var duplicate = await db.WhatsAppTemplates
            .AnyAsync(
                x => x.WhatsAppBusinessAccountId == request.WhatsAppBusinessAccountId
                     && x.Name == name
                     && x.Language == language,
                cancellationToken);

        if (duplicate)
        {
            var conflict = new ResultResponse();
            conflict.AddErrorMessage("A template with the same name and language already exists.", "integrations.templates.duplicate");
            throw new BadRequestException(conflict);
        }

        var componentsJson = JsonSerializer.Serialize(t, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var entity = new WhatsAppTemplate
        {
            Id = Guid.NewGuid(),
            MetaTemplateId = string.Empty,
            Name = name,
            Language = language,
            Category = category,
            Status = "PENDING",
            WhatsAppBusinessAccountId = request.WhatsAppBusinessAccountId,
            ComponentsJson = componentsJson,
            LastSyncedAt = DateTimeOffset.UtcNow
        };

        db.WhatsAppTemplates.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

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

        var outbound = new TemplateCreateOutboundMessage
        {
            TenantId = tenantId.ToString("N"),
            WabaId = wabaRow.WabaId.Trim(),
            Auth = new TemplateCreateOutboundAuth
            {
                AccessToken = ws.AccessToken.Trim(),
                PhoneNumberId = ws.PhoneNumberId.Trim(),
                BusinessAccountId = businessAccountId
            },
            Payload = new TemplateCreateOutboundPayload
            {
                Name = name,
                Language = language,
                Category = category,
                Components = WhatsAppTemplateMetaComponentsBuilder.Build(t)
            },
            Metadata = new TemplateCreateOutboundMetadata
            {
                CorrelationId = entity.Id.ToString("D"),
                IntegrationId = integration.Id.ToString("D"),
                WhatsAppBusinessAccountId = request.WhatsAppBusinessAccountId.ToString("D")
            }
        };

        await templatePublisher.PublishCreateTemplateAsync(outbound, cancellationToken);

        var data = new CreateWhatsAppTemplateAcceptedDto
        {
            CorrelationId = entity.Id.ToString("D"),
            Message = templatePublisher.PublishesToBroker
                ? "Template saved and queued for WhatsApp (Meta) template creation."
                : "Template saved locally. Enable RabbitMQ to publish to the template queue."
        };

        return new SingleResponse<CreateWhatsAppTemplateAcceptedDto>(data);
    }
}
