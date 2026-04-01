using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;

namespace InteractiveLeads.Infrastructure.Identity.Users;

public sealed class TeamUserTenantValidator(
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUserService) : ITeamUserTenantValidator
{
    public async Task EnsureActiveUserInCurrentTenantAsync(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tenantId = currentUserService.GetUserTenant();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            var r = new ResultResponse();
            r.AddErrorMessage("Tenant context is required.", "general.bad_request");
            throw new BadRequestException(r);
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
        {
            var r = new ResultResponse();
            r.AddErrorMessage("User not found or inactive.", "user.not_found");
            throw new BadRequestException(r);
        }

        if (!string.Equals(user.TenantId, tenantId, StringComparison.Ordinal))
        {
            var r = new ResultResponse();
            r.AddErrorMessage("User is not in the current tenant.", "general.access_denied");
            throw new BadRequestException(r);
        }
    }
}
