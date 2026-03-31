using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat;

internal static class ChatContext
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

        // Agent: requires InboxMember binding.
        if (!currentUserService.IsInRole("Agent"))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("You are not authorized to access this inbox.", "general.access_denied");
            throw new ForbiddenException(response);
        }

        var userId = currentUserService.GetUserId();
        var isMember = await db.InboxMembers
            .AsNoTracking()
            .AnyAsync(m => m.InboxId == inboxId && m.UserId == userId && m.IsActive, cancellationToken);

        if (!isMember)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("You are not authorized to access this inbox.", "general.access_denied");
            throw new ForbiddenException(response);
        }
    }

    /// <summary>
    /// Allows read/write collaboration on a conversation: Owner/Manager, or Agent if inbox member or active internal participant (Agent role).
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

        var inboxMember = await db.InboxMembers
            .AsNoTracking()
            .AnyAsync(m => m.InboxId == inboxId && m.UserId == userId && m.IsActive, cancellationToken);

        if (inboxMember)
            return;

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

        // Agent: inbox member OR internal collaborator on the conversation.
        if (currentUserService.IsInRole("Agent"))
        {
            var userId = currentUserService.GetUserId();

            return query.Where(c =>
                db.InboxMembers.Any(m =>
                    m.InboxId == c.InboxId &&
                    m.UserId == userId &&
                    m.IsActive) ||
                db.ConversationParticipants.Any(p =>
                    p.ConversationId == c.Id &&
                    p.UserId == userId &&
                    p.IsActive &&
                    p.Role == ConversationParticipantRole.Agent));
        }

        // No relevant role: deny everything.
        return query.Where(_ => false);
    }
}

