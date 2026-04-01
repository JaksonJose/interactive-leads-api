using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Inboxes.Queries;

public sealed class ListInboxesQuery : IApplicationRequest<IResponse>
{
    public bool IncludeInactive { get; set; } = true;
}

public sealed class ListInboxesQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<ListInboxesQuery, IResponse>
{
    public async Task<IResponse> Handle(ListInboxesQuery request, CancellationToken cancellationToken)
    {
        var tenantIdentifier = currentUserService.GetUserTenant();
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            return new ListResponse<InboxDto>([])
                .AddErrorMessage("Tenant context is required.", "general.bad_request");
        }

        var crmTenantId = await db.Tenants
            .Where(t => t.Identifier == tenantIdentifier)
            .Select(t => t.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (crmTenantId == Guid.Empty)
        {
            return new ListResponse<InboxDto>([])
                .AddErrorMessage("CRM tenant not found for current tenant context.", "general.not_found");
        }

        var companyId = await db.Companies
            .Where(c => c.TenantId == crmTenantId)
            .Select(c => c.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (companyId == Guid.Empty)
        {
            return new ListResponse<InboxDto>([])
                .AddErrorMessage("Company not found for current tenant.", "general.not_found");
        }

        var companyName = await db.Companies
            .Where(c => c.Id == companyId)
            .Select(c => c.Name)
            .SingleOrDefaultAsync(cancellationToken) ?? string.Empty;

        var isAgent = currentUserService.IsInRole("Agent");
        var userId = currentUserService.GetUserId();

        var query = db.Inboxes
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId);

        if (!request.IncludeInactive)
            query = query.Where(i => i.IsActive);

        if (isAgent)
        {
            query = query.Where(i => db.InboxTeams.Any(link =>
                link.InboxId == i.Id &&
                db.Teams.Any(t =>
                    t.Id == link.TeamId &&
                    t.CompanyId == companyId &&
                    t.IsActive) &&
                db.UserTeams.Any(ut => ut.TeamId == link.TeamId && ut.UserId == userId)));
        }

        var items = await query
            .OrderBy(i => i.Name)
            .Select(i => new InboxDto
            {
                Id = i.Id,
                Name = i.Name,
                CompanyName = companyName,
                IsActive = i.IsActive,
                CreatedAt = i.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new ListResponse<InboxDto>(items, items.Count);
    }
}


