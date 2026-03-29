using System.Text.Json;
using System.Text.Json.Serialization;
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

public sealed class GetWhatsAppTemplateQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    : IApplicationRequestHandler<GetWhatsAppTemplateQuery, IResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<IResponse> Handle(GetWhatsAppTemplateQuery request, CancellationToken cancellationToken)
    {
        var companyId = await CrmCompanyResolver.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var wabaOk = await db.WhatsAppBusinessAccounts
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WhatsAppBusinessAccountId && w.CompanyId == companyId, cancellationToken);

        if (!wabaOk)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("WhatsApp Business Account not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        var entity = await db.WhatsAppTemplates
            .AsNoTracking()
            .Where(t => t.Id == request.TemplateId && t.WhatsAppBusinessAccountId == request.WhatsAppBusinessAccountId)
            .SingleOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Template not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        CreateWhatsAppTemplateRequest? draft = null;
        try
        {
            draft = JsonSerializer.Deserialize<CreateWhatsAppTemplateRequest>(entity.ComponentsJson, JsonOptions);
        }
        catch (JsonException)
        {
            // Fall through with null draft
        }

        var dto = new WhatsAppTemplateDetailDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Language = entity.Language,
            Category = entity.Category,
            Status = entity.Status,
            Body = draft?.Body ?? string.Empty,
            HeaderText = string.IsNullOrWhiteSpace(draft?.HeaderText) ? null : draft!.HeaderText.Trim(),
            HeaderExample = string.IsNullOrWhiteSpace(draft?.HeaderExample) ? null : draft.HeaderExample.Trim(),
            BodyExamples = draft?.BodyExamples,
            Footer = string.IsNullOrWhiteSpace(draft?.Footer) ? null : draft!.Footer.Trim(),
            Buttons = draft?.Buttons
        };

        return new SingleResponse<WhatsAppTemplateDetailDto>(dto);
    }
}
