using System.Text.Json;
using System.Text.Json.Serialization;
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

public sealed class UpdateWhatsAppTemplateCommand : IApplicationRequest<IResponse>
{
    public Guid WhatsAppBusinessAccountId { get; set; }

    public Guid TemplateId { get; set; }

    public CreateWhatsAppTemplateRequest Template { get; set; } = new();
}

public sealed class UpdateWhatsAppTemplateCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IIntegrationSettingsResolver settingsResolver,
    ITemplateOutboundPublisher templatePublisher)
    : IApplicationRequestHandler<UpdateWhatsAppTemplateCommand, IResponse>
{
    public async Task<IResponse> Handle(UpdateWhatsAppTemplateCommand request, CancellationToken cancellationToken)
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

        var metaId = (entity.MetaTemplateId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(metaId))
        {
            var bad = new ResultResponse();
            bad.AddErrorMessage(
                "Template has no Meta id yet; wait for creation or sync before updating on WhatsApp.",
                "integrations.templates.update_requires_meta_id");
            throw new BadRequestException(bad);
        }

        var metaStatus = (entity.Status ?? string.Empty).Trim();
        if (!string.Equals(metaStatus, "APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            var bad = new ResultResponse();
            bad.AddErrorMessage(
                "Template can only be edited when Meta status is APPROVED.",
                "integrations.templates.update_requires_approved_status");
            throw new BadRequestException(bad);
        }

        var t = request.Template;
        var name = (t.Name ?? string.Empty).Trim();
        var language = (t.Language ?? string.Empty).Trim();
        var category = (t.Category ?? string.Empty).Trim().ToUpperInvariant();
        var body = (t.Body ?? string.Empty).Trim();

        if (!string.Equals(name, entity.Name.Trim(), StringComparison.Ordinal) ||
            !string.Equals(language, entity.Language.Trim(), StringComparison.Ordinal))
        {
            var mismatch = new ResultResponse();
            mismatch.AddErrorMessage(
                "Template name and language cannot be changed when updating; edit content, category, or buttons only.",
                "integrations.templates.update_name_language_locked");
            throw new BadRequestException(mismatch);
        }

        var badRequest = new ResultResponse();
        if (name.Length is < 1 or > 512)
            badRequest.AddErrorMessage("Template name length is invalid.", "integrations.templates.invalid_name");
        if (language.Length is < 1 or > 32)
            badRequest.AddErrorMessage("Template language is invalid.", "integrations.templates.invalid_language");

        if (badRequest.Messages is { Count: > 0 })
            throw new BadRequestException(badRequest);

        var headerText = string.IsNullOrWhiteSpace(t.HeaderText) ? null : t.HeaderText.Trim();
        var headerExample = string.IsNullOrWhiteSpace(t.HeaderExample) ? null : t.HeaderExample.Trim();
        var bodyExamples = t.BodyExamples;
        var compileError = WhatsAppTemplatePlaceholderCompiler.TryCompileAndApply(
            ref headerText,
            ref body,
            ref headerExample,
            ref bodyExamples,
            t.AuthoringHeaderText,
            t.AuthoringBody,
            out var persistedAuthoringHeader,
            out var persistedAuthoringBody,
            out var variableBindings);
        if (compileError != null)
            throw new BadRequestException(compileError);

        var footer = string.IsNullOrWhiteSpace(t.Footer) ? null : t.Footer.Trim();
        var fieldErrors = WhatsAppTemplateMutationValidation.ValidateEditableFields(
            category,
            body,
            headerText,
            footer,
            t.Buttons);
        if (fieldErrors != null)
            badRequest.Messages.AddRange(fieldErrors.Messages);

        var exampleErrors = WhatsAppTemplateMutationValidation.ValidateMetaPlaceholderExamples(
            headerText,
            headerExample,
            body,
            bodyExamples);
        if (exampleErrors != null)
            badRequest.Messages.AddRange(exampleErrors.Messages);

        if (badRequest.Messages is { Count: > 0 })
            throw new BadRequestException(badRequest);

        var persisted = new WhatsAppTemplatePersistedComponents
        {
            SchemaVersion = 1,
            Name = name,
            Language = language,
            Category = category,
            AuthoringHeaderText = persistedAuthoringHeader,
            AuthoringBody = persistedAuthoringBody,
            HeaderText = headerText,
            HeaderExample = headerExample,
            Body = body,
            BodyExamples = bodyExamples,
            Footer = footer,
            Buttons = t.Buttons,
            VariableBindings = variableBindings.Count > 0 ? variableBindings.ToArray() : null,
            IsMetaSynced = false
        };

        var forMetaBuild = new CreateWhatsAppTemplateRequest
        {
            Name = name,
            Language = language,
            Category = category,
            HeaderText = headerText,
            HeaderExample = headerExample,
            Body = body,
            BodyExamples = bodyExamples,
            Footer = footer,
            Buttons = t.Buttons
        };

        var componentsJson = JsonSerializer.Serialize(persisted, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        entity.Category = category;
        entity.ComponentsJson = componentsJson;
        entity.Status = "PENDING";
        entity.SubmissionLastError = null;
        entity.SubmissionLastErrorCode = null;
        entity.SubmissionLastErrorAt = null;
        entity.LastSyncedAt = DateTimeOffset.UtcNow;

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

        var outbound = new TemplateUpdateOutboundMessage
        {
            TenantId = tenantId.ToString("N"),
            WabaId = wabaRow.WabaId.Trim(),
            Auth = new TemplateCreateOutboundAuth
            {
                AccessToken = ws.AccessToken.Trim(),
                PhoneNumberId = ws.PhoneNumberId.Trim(),
                BusinessAccountId = businessAccountId
            },
            Payload = new TemplateUpdateOutboundPayload
            {
                MetaTemplateId = metaId,
                Name = name,
                Language = language,
                Category = category,
                Components = WhatsAppTemplateMetaComponentsBuilder.Build(forMetaBuild)
            },
            Metadata = new TemplateCreateOutboundMetadata
            {
                CorrelationId = entity.Id.ToString("D"),
                IntegrationId = integration.Id.ToString("D"),
                WhatsAppBusinessAccountId = request.WhatsAppBusinessAccountId.ToString("D")
            }
        };

        string message;
        try
        {
            await templatePublisher.PublishUpdateTemplateAsync(outbound, cancellationToken);
            message = templatePublisher.PublishesToBroker
                ? "Template update queued for WhatsApp (Meta)."
                : "Template saved locally. Enable RabbitMQ to publish update_template jobs.";
        }
        catch (Exception ex)
        {
            entity.SubmissionLastError = ex.Message;
            entity.SubmissionLastErrorCode = "rabbitmq_publish_failed";
            entity.SubmissionLastErrorAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            message = "Template saved, but queuing the update for WhatsApp (Meta) failed. You can edit and try again.";
        }

        var data = new CreateWhatsAppTemplateAcceptedDto
        {
            CorrelationId = entity.Id.ToString("D"),
            Message = message
        };

        return new SingleResponse<CreateWhatsAppTemplateAcceptedDto>(data);
    }
}
