using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.Teams.Commands;

public sealed class CreateTeamCommand : IApplicationRequest<IResponse>
{
    public CreateTeamRequest CreateTeam { get; set; } = new();
}

public sealed class CreateTeamCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<CreateTeamCommand, IResponse>
{
    public async Task<IResponse> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var tenantId = await db.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => c.TenantId)
            .SingleAsync(cancellationToken);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CompanyId = companyId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            team.Rename(request.CreateTeam?.Name ?? string.Empty);
            team.SetDescription(request.CreateTeam?.Description);
        }
        catch (ArgumentException ex)
        {
            var response = new ResultResponse();
            response.AddErrorMessage(ex.Message, "teams.validation");
            throw new BadRequestException(response);
        }

        db.Teams.Add(team);
        await db.SaveChangesAsync(cancellationToken);

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
            MemberCount = 0
        });
    }
}
