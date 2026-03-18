using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Inboxes.Queries;

public sealed class GetInboxQuery : IRequest<IResponse>
{
    public Guid InboxId { get; set; }
}

public sealed class GetInboxQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IRequestHandler<GetInboxQuery, IResponse>
{
    public async Task<IResponse> Handle(GetInboxQuery request, CancellationToken cancellationToken)
    {
        var tenantIdentifier = currentUserService.GetUserTenant();
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Tenant context is required.", "general.bad_request");
            throw new BadRequestException(response);
        }

        var crmTenantId = await db.Tenants
            .Where(t => t.Identifier == tenantIdentifier)
            .Select(t => t.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (crmTenantId == Guid.Empty)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("CRM tenant not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        var companyId = await db.Companies
            .Where(c => c.TenantId == crmTenantId)
            .Select(c => c.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (companyId == Guid.Empty)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Company not found for current tenant.", "general.not_found");
            throw new NotFoundException(response);
        }

        var inbox = await db.Inboxes
            .AsNoTracking()
            .Where(i => i.Id == request.InboxId && i.CompanyId == companyId)
            .Select(i => new InboxDto
            {
                Id = i.Id,
                Name = i.Name,
                IsActive = i.IsActive,
                CreatedAt = i.CreatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (inbox == null)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Inbox not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        if (currentUserService.IsInRole("Agent"))
        {
            var userId = currentUserService.GetUserId();
            var hasMembership = await db.InboxMembers
                .AsNoTracking()
                .AnyAsync(m => m.InboxId == inbox.Id && m.UserId == userId && m.IsActive, cancellationToken);

            if (!hasMembership)
            {
                var response = new ResultResponse();
                response.AddErrorMessage("You are not authorized to access this inbox.", "general.access_denied");
                throw new ForbiddenException(response);
            }
        }

        return new SingleResponse<InboxDto>(inbox);
    }
}

