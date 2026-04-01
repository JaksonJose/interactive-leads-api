using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat;

public static class ChatContext
{
    public static async Task<Guid> GetCompanyIdAsync(
        IApplicationDbContext db,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        var tenantIdentifier = currentUserService.GetUserTenant();
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Tenant context is required.", "general.bad_request");
            throw new BadRequestException(response);
        }

        var crmTenantId = await db.Tenants
            .Where(t => t.Identifier == tenantIdentifier)
            .Select(t => t.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (crmTenantId == Guid.Empty)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("CRM tenant not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        var companyId = await db.Companies
            .Where(c => c.TenantId == crmTenantId)
            .Select(c => c.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (companyId == Guid.Empty)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Company not found for current tenant.", "general.not_found");
            throw new NotFoundException(response);
        }

        return companyId;
    }

    public static async Task EnsureInboxAccessAsync(
        IApplicationDbContext db,
        ICurrentUserService currentUserService,
        Guid inboxId,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var exists = await db.Inboxes
            .AsNoTracking()
            .AnyAsync(i => i.Id == inboxId && i.CompanyId == companyId, cancellationToken);

        if (!exists)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Inbox not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        // Owner/Manager: full access within the tenant (ignore Agent bindings).
        if (currentUserService.IsInRole("Owner") || currentUserService.IsInRole("Manager"))
            return;

        // Agent: must belong to an active team linked to this inbox (InboxTeam).
        if (!currentUserService.IsInRole("Agent"))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("You are not authorized to access this inbox.", "general.access_denied");
            throw new ForbiddenException(response);
        }

        var userId = currentUserService.GetUserId();
        var isMember = await AgentHasInboxAccessViaTeamsAsync(db, userId, inboxId, companyId, cancellationToken);

        if (!isMember)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("You are not authorized to access this inbox.", "general.access_denied");
            throw new ForbiddenException(response);
        }
    }

    /// <summary>
    /// Allows read/write on a conversation: Owner/Manager; or Agent if responsible, unassigned (and team-linked to inbox), or active internal participant.
    /// </summary>
    public static async Task EnsureConversationCollaborationAccessAsync(
        IApplicationDbContext db,
        ICurrentUserService currentUserService,
        Guid conversationId,
        Guid inboxId,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var inboxExists = await db.Inboxes
            .AsNoTracking()
            .AnyAsync(i => i.Id == inboxId && i.CompanyId == companyId, cancellationToken);

        if (!inboxExists)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Inbox not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        if (currentUserService.IsInRole("Owner") || currentUserService.IsInRole("Manager"))
            return;

        if (!currentUserService.IsInRole("Agent"))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("You are not authorized to access this conversation.", "general.access_denied");
            throw new ForbiddenException(response);
        }

        var userId = currentUserService.GetUserId();

        var asParticipant = await db.ConversationParticipants
            .AsNoTracking()
            .AnyAsync(p =>
                    p.ConversationId == conversationId &&
                    p.UserId == userId &&
                    p.IsActive &&
                    p.Role == ConversationParticipantRole.Agent,
                cancellationToken);

        if (asParticipant)
            return;

        if (!Guid.TryParse(userId, out var userGuid))
        {
            var forbiddenInvalid = new ResultResponse();
            forbiddenInvalid.AddErrorMessage("You are not authorized to access this conversation.", "general.access_denied");
            throw new ForbiddenException(forbiddenInvalid);
        }

        var convRow = await db.Conversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId && c.CompanyId == companyId && c.InboxId == inboxId)
            .Select(c => new { c.AssignedAgentId })
            .FirstOrDefaultAsync(cancellationToken);

        if (convRow is null)
        {
            var notFoundConv = new ResultResponse();
            notFoundConv.AddErrorMessage("Conversation not found.", "general.not_found");
            throw new NotFoundException(notFoundConv);
        }

        if (convRow.AssignedAgentId == userGuid)
            return;

        if (!convRow.AssignedAgentId.HasValue)
        {
            if (await AgentHasInboxAccessViaTeamsAsync(db, userId, inboxId, companyId, cancellationToken))
                return;
        }

        var forbidden = new ResultResponse();
        forbidden.AddErrorMessage("You are not authorized to access this conversation.", "general.access_denied");
        throw new ForbiddenException(forbidden);
    }

    public static IQueryable<Conversation> ApplyConversationAccessFilter(
        IApplicationDbContext db,
        ICurrentUserService currentUserService,
        Guid companyId,
        IQueryable<Conversation> query)
    {
        query = query.Where(c => c.CompanyId == companyId);

        // Owner/Manager: unrestricted within the tenant.
        if (currentUserService.IsInRole("Owner") || currentUserService.IsInRole("Manager"))
            return query;

        // Agent: unassigned (and team-linked to inbox) OR assigned to self OR active internal participant.
        if (currentUserService.IsInRole("Agent"))
        {
            var userId = currentUserService.GetUserId();
            if (!Guid.TryParse(userId, out var userGuid))
                return query.Where(_ => false);

            return query.Where(c =>
                c.AssignedAgentId == userGuid
                || db.ConversationParticipants.Any(p =>
                    p.ConversationId == c.Id &&
                    p.UserId == userId &&
                    p.IsActive &&
                    p.Role == ConversationParticipantRole.Agent)
                || (c.AssignedAgentId == null &&
                    db.InboxTeams.Any(link =>
                        link.InboxId == c.InboxId &&
                        db.Teams.Any(t =>
                            t.Id == link.TeamId &&
                            t.CompanyId == companyId &&
                            t.IsActive) &&
                        db.UserTeams.Any(ut => ut.TeamId == link.TeamId && ut.UserId == userId))));
        }

        // No relevant role: deny everything.
        return query.Where(_ => false);
    }

    /// <summary>
    /// When <paramref name="teamId"/> is set, returns assigned agent user ids for that team.
    /// If the team is missing or inactive for the company, returns an empty set (caller should yield no rows).
    /// When <paramref name="teamId"/> is null, returns null (no team filter).
    /// </summary>
    public static async Task<List<Guid>?> TryResolveTeamAssignedAgentGuidsAsync(
        IApplicationDbContext db,
        Guid companyId,
        Guid? teamId,
        CancellationToken cancellationToken)
    {
        if (!teamId.HasValue || teamId.Value == Guid.Empty)
            return null;

        var teamOk = await db.Teams
            .AsNoTracking()
            .AnyAsync(t => t.Id == teamId.Value && t.CompanyId == companyId && t.IsActive, cancellationToken);

        if (!teamOk)
            return [];

        var userIds = await db.UserTeams
            .AsNoTracking()
            .Where(m => m.TeamId == teamId.Value)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        var guids = new List<Guid>(userIds.Count);
        foreach (var s in userIds)
        {
            if (Guid.TryParse(s, out var g))
                guids.Add(g);
        }

        return guids;
    }

    /// <summary>
    /// Whether the agent is on at least one active team in <paramref name="companyId"/> linked to the inbox.
    /// </summary>
    public static Task<bool> AgentHasInboxAccessViaTeamsAsync(
        IApplicationDbContext db,
        string userId,
        Guid inboxId,
        Guid companyId,
        CancellationToken cancellationToken) =>
        db.InboxTeams.AsNoTracking()
            .AnyAsync(
                link => link.InboxId == inboxId &&
                    db.Teams.Any(t =>
                        t.Id == link.TeamId &&
                        t.CompanyId == companyId &&
                        t.IsActive) &&
                    db.UserTeams.Any(ut => ut.TeamId == link.TeamId && ut.UserId == userId),
                cancellationToken);
}

