using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.SlaPolicies.Queries;

public sealed class GetSlaPolicyByIdQuery : IApplicationRequest<IResponse>
{
    public Guid PolicyId { get; set; }
}

public sealed class GetSlaPolicyByIdQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<GetSlaPolicyByIdQuery, IResponse>
{
    public async Task<IResponse> Handle(GetSlaPolicyByIdQuery request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var policy = await db.SlaPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PolicyId && p.CompanyId == companyId, cancellationToken);

        if (policy is null)
        {
            var nf = new ResultResponse();
            nf.AddErrorMessage("SLA policy not found.", "general.not_found");
            throw new NotFoundException(nf);
        }

        return new SingleResponse<SlaPolicyDto>(SlaPolicyMapping.ToDto(policy));
    }
}
