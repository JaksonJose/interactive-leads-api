using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Teams.Commands;

public sealed class UpdateTeamCommand : IApplicationRequest<IResponse>
{
    public Guid TeamId { get; set; }
    public UpdateTeamRequest UpdateTeam { get; set; } = new();
}

public sealed class UpdateTeamCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<UpdateTeamCommand, IResponse>
{
    public async Task<IResponse> Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
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

        if (!team.IsActive)
        {
            var bad = new ResultResponse();
            bad.AddErrorMessage("Team is inactive.", "teams.inactive");
            throw new BadRequestException(bad);
        }

        try
        {
            if (request.UpdateTeam.Name is not null)
                team.Rename(request.UpdateTeam.Name);
            if (request.UpdateTeam.Description is not null)
                team.SetDescription(request.UpdateTeam.Description);
            if (request.UpdateTeam.IsActive.HasValue)
                team.IsActive = request.UpdateTeam.IsActive.Value;
            if (request.UpdateTeam.CalendarId.HasValue)
                team.CalendarId = request.UpdateTeam.CalendarId;
            if (request.UpdateTeam.SlaPolicyId.HasValue)
                team.SlaPolicyId = request.UpdateTeam.SlaPolicyId;
            if (request.UpdateTeam.AutoAssignEnabled.HasValue)
                team.AutoAssignEnabled = request.UpdateTeam.AutoAssignEnabled.Value;
            if (request.UpdateTeam.AutoAssignStrategy.HasValue)
                team.AutoAssignStrategy = request.UpdateTeam.AutoAssignStrategy.Value;
            if (request.UpdateTeam.AutoAssignIgnoreOfflineUsers.HasValue)
                team.AutoAssignIgnoreOfflineUsers = request.UpdateTeam.AutoAssignIgnoreOfflineUsers.Value;
            if (request.UpdateTeam.AutoAssignMaxConversationsPerUser.HasValue)
                team.AutoAssignMaxConversationsPerUser = request.UpdateTeam.AutoAssignMaxConversationsPerUser;
            if (request.UpdateTeam.AutoAssignReassignTimeoutMinutes.HasValue)
                team.AutoAssignReassignTimeoutMinutes = request.UpdateTeam.AutoAssignReassignTimeoutMinutes;
        }
        catch (ArgumentException ex)
        {
            var response = new ResultResponse();
            response.AddErrorMessage(ex.Message, "teams.validation");
            throw new BadRequestException(response);
        }

        await db.SaveChangesAsync(cancellationToken);

        var memberCount = await db.UserTeams
            .AsNoTracking()
            .CountAsync(m => m.TeamId == team.Id, cancellationToken);

        return new SingleResponse<TeamDto>(new TeamDto
        {
            Id = team.Id,
            CompanyId = team.CompanyId,
            TenantId = team.TenantId,
            Name = team.Name,
            Description = team.Description,
            IsActive = team.IsActive,
            CreatedAt = team.CreatedAt,
            CalendarId = team.CalendarId,
            SlaPolicyId = team.SlaPolicyId,
            MemberCount = memberCount,
            AutoAssignEnabled = team.AutoAssignEnabled,
            AutoAssignStrategy = team.AutoAssignStrategy,
            AutoAssignIgnoreOfflineUsers = team.AutoAssignIgnoreOfflineUsers,
            AutoAssignMaxConversationsPerUser = team.AutoAssignMaxConversationsPerUser,
            AutoAssignReassignTimeoutMinutes = team.AutoAssignReassignTimeoutMinutes
        });
    }
}
