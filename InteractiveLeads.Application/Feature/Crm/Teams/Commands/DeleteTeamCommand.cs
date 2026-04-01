using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Teams.Commands;

public sealed class DeleteTeamCommand : IApplicationRequest<IResponse>
{
    public Guid TeamId { get; set; }
}

public sealed class DeleteTeamCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<DeleteTeamCommand, IResponse>
{
    public async Task<IResponse> Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var team = await db.Teams
            .FirstOrDefaultAsync(t => t.Id == request.TeamId && t.CompanyId == companyId, cancellationToken);

        if (team is null)
        {
            var nf = new ResultResponse();
            nf.AddErrorMessage("Team not found.", "general.not_found");
            throw new NotFoundException(nf);
        }

        team.Deactivate();
        await db.SaveChangesAsync(cancellationToken);

        return new ResultResponse();
    }
}
