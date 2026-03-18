using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Integrations.Commands;

public sealed class DeleteIntegrationCommand : IRequest<IResponse>
{
    public Guid IntegrationId { get; set; }
}

public sealed class DeleteIntegrationCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IIntegrationExternalIdentifierLookupRepository integrationLookupRepository) : IRequestHandler<DeleteIntegrationCommand, IResponse>
{
    public async Task<IResponse> Handle(DeleteIntegrationCommand request, CancellationToken cancellationToken)
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
            .Where(i => i.Id == request.IntegrationId && i.CompanyId == companyId)
            .SingleOrDefaultAsync(cancellationToken);

        if (integration == null)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Integration not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        await integrationLookupRepository.RemoveByIntegrationIdAsync(integration.Id, cancellationToken);

        db.Integrations.Remove(integration);
        await db.SaveChangesAsync(cancellationToken);

        return new ResultResponse();
    }
}
