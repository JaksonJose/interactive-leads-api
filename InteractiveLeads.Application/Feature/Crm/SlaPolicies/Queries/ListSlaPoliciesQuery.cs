using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.SlaPolicies.Queries;

public sealed class ListSlaPoliciesQuery : IApplicationRequest<IResponse>
{
    /// <summary>When true, only active policies. When false, only inactive. When null, all.</summary>
    public bool? ActiveOnly { get; set; }

    /// <summary>Optional lower bound for UpdatedAt (reporting / sync).</summary>
    public DateTimeOffset? UpdatedAfter { get; set; }
}

public sealed class ListSlaPoliciesQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<ListSlaPoliciesQuery, IResponse>
{
    public async Task<IResponse> Handle(ListSlaPoliciesQuery request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var query = db.SlaPolicies.AsNoTracking().Where(p => p.CompanyId == companyId);

        if (request.ActiveOnly == true)
            query = query.Where(p => p.IsActive);
        else if (request.ActiveOnly == false)
            query = query.Where(p => !p.IsActive);

        if (request.UpdatedAfter.HasValue)
            query = query.Where(p => p.UpdatedAt >= request.UpdatedAfter.Value);

        var rows = await query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var items = rows.ConvertAll(SlaPolicyMapping.ToDto);

        return new ListResponse<SlaPolicyDto>(items, items.Count);
    }
}
