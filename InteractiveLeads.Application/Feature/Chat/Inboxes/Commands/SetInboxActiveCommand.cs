using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Inboxes.Commands;

public sealed class SetInboxActiveCommand : IRequest<IResponse>
{
    public Guid InboxId { get; set; }
    public bool IsActive { get; set; }
}

public sealed class SetInboxActiveCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IRequestHandler<SetInboxActiveCommand, IResponse>
{
    public async Task<IResponse> Handle(SetInboxActiveCommand request, CancellationToken cancellationToken)
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
            .Where(i => i.Id == request.InboxId && i.CompanyId == companyId)
            .SingleOrDefaultAsync(cancellationToken);

        if (inbox == null)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Inbox not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        inbox.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);

        return new ResultResponse();
    }
}

