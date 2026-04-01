using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Teams.Commands;

public sealed class AddUserToTeamCommand : IApplicationRequest<IResponse>
{
    public Guid TeamId { get; set; }
    public AddUserToTeamRequest AddUser { get; set; } = new();
}

public sealed class AddUserToTeamCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    ITeamUserTenantValidator userTenantValidator) : IApplicationRequestHandler<AddUserToTeamCommand, IResponse>
{
    public async Task<IResponse> Handle(AddUserToTeamCommand request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var team = await db.Teams
            .FirstOrDefaultAsync(t => t.Id == request.TeamId && t.CompanyId == companyId, cancellationToken);

        if (team is null || !team.IsActive)
        {
            var nf = new ResultResponse();
            nf.AddErrorMessage("Team not found.", "general.not_found");
            throw new NotFoundException(nf);
        }

        var userId = (request.AddUser?.UserId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(userId))
        {
            var bad = new ResultResponse();
            bad.AddErrorMessage("UserId is required.", "teams.user_required");
            throw new BadRequestException(bad);
        }

        await userTenantValidator.EnsureActiveUserInCurrentTenantAsync(userId, cancellationToken);

        var exists = await db.UserTeams
            .AnyAsync(m => m.TeamId == team.Id && m.UserId == userId, cancellationToken);
        if (exists)
        {
            var bad = new ResultResponse();
            bad.AddErrorMessage("User is already in this team.", "teams.duplicate_member");
            throw new BadRequestException(bad);
        }

        db.UserTeams.Add(new UserTeam
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            UserId = userId,
            Role = request.AddUser?.Role,
            JoinedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        return new ResultResponse();
    }
}
