namespace InteractiveLeads.Application.Interfaces;

/// <summary>Validates that an identity user may be added to a team (active, same Finbuckle tenant).</summary>
public interface ITeamUserTenantValidator
{
    Task EnsureActiveUserInCurrentTenantAsync(string userId, CancellationToken cancellationToken);
}
