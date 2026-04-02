using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.SlaPolicies.Commands;

public sealed class DeactivateSlaPolicyCommand : IApplicationRequest<IResponse>
{
    public Guid PolicyId { get; set; }
}

public sealed class DeactivateSlaPolicyCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<DeactivateSlaPolicyCommand, IResponse>
{
    public async Task<IResponse> Handle(DeactivateSlaPolicyCommand request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var policy = await db.SlaPolicies
            .FirstOrDefaultAsync(p => p.Id == request.PolicyId && p.CompanyId == companyId, cancellationToken);

        if (policy is null)
        {
            var nf = new ResultResponse();
            nf.AddErrorMessage("SLA policy not found.", "general.not_found");
            throw new NotFoundException(nf);
        }

        policy.IsActive = false;
        policy.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return new SingleResponse<SlaPolicyDto>(SlaPolicyMapping.ToDto(policy));
    }
}
