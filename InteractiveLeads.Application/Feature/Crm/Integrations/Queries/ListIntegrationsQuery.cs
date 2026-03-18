using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Integrations.Settings;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Integrations.Queries;

public sealed class ListIntegrationsQuery : IRequest<IResponse>
{
    public bool IncludeInactive { get; set; } = true;
}

public sealed class ListIntegrationsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IIntegrationSettingsResolver settingsResolver) : IRequestHandler<ListIntegrationsQuery, IResponse>
{
    public async Task<IResponse> Handle(ListIntegrationsQuery request, CancellationToken cancellationToken)
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

        var query = db.Integrations
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId);

        if (!request.IncludeInactive)
        {
            query = query.Where(i => i.IsActive);
        }

        var integrations = await query
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);

        var items = integrations
            .Select(i => IntegrationDtoMapper.ToResponse(i, settingsResolver))
            .ToList();

        return new ListResponse<IntegrationResponse>(items);
    }
}

