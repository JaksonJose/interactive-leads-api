using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Infrastructure.Context.Application;
using InteractiveLeads.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Identity.Users;

public sealed class ChatConversationUserValidator(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUserService) : IChatConversationUserValidator
{
    public async Task EnsureValidResponsibleTargetAsync(Guid targetUserId, Guid inboxId, CancellationToken cancellationToken)
    {
        var tenantId = currentUserService.GetUserTenant();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            var r = new ResultResponse();
            r.AddErrorMessage("Tenant context is required.", "general.bad_request");
            throw new BadRequestException(r);
        }

        var user = await userManager.FindByIdAsync(targetUserId.ToString());
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

        if (!await userManager.IsInRoleAsync(user, "Agent"))
        {
            var r = new ResultResponse();
            r.AddErrorMessage("Responsible must be a user with Agent role.", "chat.responsible_must_be_agent");
            throw new BadRequestException(r);
        }

        var inboxCompanyId = await db.Inboxes
            .AsNoTracking()
            .Where(i => i.Id == inboxId)
            .Select(i => i.CompanyId)
            .SingleOrDefaultAsync(cancellationToken);

        if (inboxCompanyId == Guid.Empty)
        {
            var r = new ResultResponse();
            r.AddErrorMessage("Inbox not found.", "general.not_found");
            throw new NotFoundException(r);
        }

        var hasTeamAccess = await ChatContext.AgentHasInboxAccessViaTeamsAsync(
            db,
            user.Id.ToString(),
            inboxId,
            inboxCompanyId,
            cancellationToken);

        if (!hasTeamAccess)
        {
            var r = new ResultResponse();
            r.AddErrorMessage(
                "Responsible must belong to an active team linked to this inbox.",
                "chat.responsible_must_have_team_inbox_access");
            throw new BadRequestException(r);
        }
    }

    public async Task EnsureValidParticipantTargetAsync(Guid targetUserId, CancellationToken cancellationToken)
    {
        var tenantId = currentUserService.GetUserTenant();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            var r = new ResultResponse();
            r.AddErrorMessage("Tenant context is required.", "general.bad_request");
            throw new BadRequestException(r);
        }

        var user = await userManager.FindByIdAsync(targetUserId.ToString());
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
