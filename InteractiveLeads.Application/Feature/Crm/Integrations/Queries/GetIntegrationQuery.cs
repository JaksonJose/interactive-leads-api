using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Integrations.Settings;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Integrations.Queries;

public sealed class GetIntegrationQuery : IRequest<IResponse>
{
    public Guid IntegrationId { get; set; }
}

public sealed class GetIntegrationQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IIntegrationSettingsResolver settingsResolver) : IRequestHandler<GetIntegrationQuery, IResponse>
{
    public async Task<IResponse> Handle(GetIntegrationQuery request, CancellationToken cancellationToken)
    {
        var tenantIdentifier = currentUserService.GetUserTenant();
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            var badRequest = new ResultResponse();
            badRequest.AddErrorMessage("Tenant context is required.", "general.bad_request");
            throw new BadRequestException(badRequest);
        }

        var crmTenantId = await db.Tenants
            .Where(t => t.Identifier == tenantIdentifier)
            .Select(t => t.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (crmTenantId == Guid.Empty)
        {
            var notFound = new ResultResponse();
            notFound.AddErrorMessage("CRM tenant not found.", "general.not_found");
            throw new NotFoundException(notFound);
        }

        var companyId = await db.Companies
            .Where(c => c.TenantId == crmTenantId)
            .Select(c => c.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (companyId == Guid.Empty)
        {
            var notFound = new ResultResponse();
            notFound.AddErrorMessage("Company not found for current tenant.", "general.not_found");
            throw new NotFoundException(notFound);
        }

        var integration = await db.Integrations
            .AsNoTracking()
            .Where(i => i.Id == request.IntegrationId && i.CompanyId == companyId)
            .SingleOrDefaultAsync(cancellationToken);

        if (integration == null)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Integration not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        var dto = IntegrationDtoMapper.ToResponse(integration, settingsResolver);
        return new SingleResponse<IntegrationResponse>(dto);
    }
}

