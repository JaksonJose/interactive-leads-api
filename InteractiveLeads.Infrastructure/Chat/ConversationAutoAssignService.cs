using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Realtime.Services.Presence;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Domain.Enums;
using InteractiveLeads.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace InteractiveLeads.Infrastructure.Chat;

public sealed class ConversationAutoAssignService(
    IApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IPresenceService presenceService,
    IAutoAssignRoundRobinStore roundRobinStore,
    IConversationCollaborationRealtimePublisher collaborationRealtime,
    IConversationSlaService conversationSlaService,
    ILogger<ConversationAutoAssignService> logger) : IConversationAutoAssignService
{
    public async Task TryAssignNewConversationAsync(
        Guid companyId,
        string tenantIdentifier,
        Conversation conversation,
        CancellationToken cancellationToken)
    {
        if (conversation.AssignedAgentId.HasValue)
            return;

        var teamIds = await db.InboxTeams
            .AsNoTracking()
            .Where(l => l.InboxId == conversation.InboxId)
            .OrderBy(l => l.Priority)
            .Select(l => l.TeamId)
            .ToListAsync(cancellationToken);

        foreach (var teamId in teamIds)
        {
            var team = await db.Teams
                .FirstOrDefaultAsync(t => t.Id == teamId && t.CompanyId == companyId, cancellationToken);

            if (team is null || !team.IsActive)
                continue;

            if (!team.AutoAssignEnabled)
                continue;

            var candidates = await GetEligibleAgentsAsync(team, conversation.InboxId, tenantIdentifier, cancellationToken);
            if (candidates.Count == 0)
                continue;

            Guid? chosen = team.AutoAssignStrategy switch
            {
                AutoAssignStrategy.RoundRobin => await PickRoundRobinAsync(team.Id, candidates, cancellationToken),
                AutoAssignStrategy.LeastAssigned => await PickLeastAssignedAsync(conversation.InboxId, candidates, cancellationToken),
                AutoAssignStrategy.Random => candidates[Random.Shared.Next(candidates.Count)],
                _ => await PickRoundRobinAsync(team.Id, candidates, cancellationToken)
            };

            if (chosen is null)
                continue;

            conversation.AssignedAgentId = chosen;
            conversation.AssignedAt = DateTimeOffset.UtcNow;
            conversation.HandlingTeamId = team.Id;

            await EnsureAgentParticipantAsync(conversation.Id, chosen.Value, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            // Collaboration realtime for new inbound conversations is published after DB commit (ProcessInboundEventCommandHandler).
            return;
        }
    }

    public async Task<bool> TryReassignAfterFirstResponseSlaExpiredAsync(
        Guid companyId,
        string tenantIdentifier,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await db.Conversations
            .Include(c => c.HandlingTeam)
            .Include(c => c.Inbox)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.CompanyId == companyId, cancellationToken);

        if (conversation is null)
            return false;

        if (conversation.Status != ConversationStatus.Open)
            return false;

        if (!conversation.AssignedAgentId.HasValue)
            return false;

        if (conversation.FirstAgentResponseAt.HasValue)
            return false;

        var utc = DateTimeOffset.UtcNow;
        if (!conversation.FirstResponseDueAt.HasValue || conversation.FirstResponseDueAt.Value >= utc)
            return false;

        var team = conversation.HandlingTeam;
        if (team is null || !team.IsActive)
            return false;

        if (!team.AutoAssignEnabled || !team.AutoReassignOnFirstResponseSlaExpired)
            return false;

        return await ReassignToNextAgentExcludingCurrentAsync(
            conversation,
            team,
            tenantIdentifier,
            conversationId,
            "SLA reassign",
            cancellationToken);
    }

    public async Task<bool> TryReassignAfterCustomerMessageInactivityAsync(
        Guid companyId,
        string tenantIdentifier,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await db.Conversations
            .Include(c => c.HandlingTeam)
            .Include(c => c.Inbox)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.CompanyId == companyId, cancellationToken);

        if (conversation is null)
            return false;

        if (conversation.Status != ConversationStatus.Open)
            return false;

        if (!conversation.AssignedAgentId.HasValue)
            return false;

        if (!conversation.FirstAgentResponseAt.HasValue)
            return false;

        if (!conversation.LastMessageFromCustomer)
            return false;

        var team = conversation.HandlingTeam;
        if (team is null || !team.IsActive)
            return false;

        if (!team.AutoAssignEnabled)
            return false;

        var timeoutMinutes = team.AutoAssignReassignTimeoutMinutes;
        if (timeoutMinutes is null or <= 0)
            return false;

        var utc = DateTimeOffset.UtcNow;
        if (conversation.LastMessageAt.AddMinutes(timeoutMinutes.Value) > utc)
            return false;

        return await ReassignToNextAgentExcludingCurrentAsync(
            conversation,
            team,
            tenantIdentifier,
            conversationId,
            "Inactivity reassign",
            cancellationToken);
    }

    private async Task<bool> ReassignToNextAgentExcludingCurrentAsync(
        Conversation conversation,
        Team team,
        string tenantIdentifier,
        Guid conversationId,
        string logLabel,
        CancellationToken cancellationToken)
    {
        var utc = DateTimeOffset.UtcNow;
        var currentId = conversation.AssignedAgentId!.Value;

        var candidates = await GetEligibleAgentsAsync(
            team,
            conversation.InboxId,
            tenantIdentifier,
            cancellationToken,
            currentId);

        if (candidates.Count == 0)
        {
            logger.LogInformation(
                "{LogLabel} skipped: no eligible agents besides current. ConversationId {ConversationId} TeamId {TeamId}",
                logLabel,
                conversationId,
                team.Id);
            return false;
        }

        Guid? chosen = team.AutoAssignStrategy switch
        {
            AutoAssignStrategy.RoundRobin => await PickRoundRobinAsync(team.Id, candidates, cancellationToken),
            AutoAssignStrategy.LeastAssigned => await PickLeastAssignedAsync(conversation.InboxId, candidates, cancellationToken),
            AutoAssignStrategy.Random => candidates[Random.Shared.Next(candidates.Count)],
            _ => await PickRoundRobinAsync(team.Id, candidates, cancellationToken)
        };

        if (chosen is null || chosen.Value == currentId)
            return false;

        conversation.AssignedAgentId = chosen;
        conversation.AssignedAt = utc;

        await EnsureAgentParticipantAsync(conversation.Id, chosen.Value, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await conversationSlaService.ApplySlaDeadlinesAsync(conversation.Id, cancellationToken, utc);

        try
        {
            await collaborationRealtime.PublishCollaborationUpdatedAsync(conversation, tenantIdentifier, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish collaboration update after {LogLabel}.", logLabel);
        }

        return true;
    }

    private async Task<List<Guid>> GetEligibleAgentsAsync(
        Team team,
        Guid inboxId,
        string tenantIdentifier,
        CancellationToken cancellationToken,
        Guid? excludeUserId = null)
    {
        var memberUserIds = await db.UserTeams
            .AsNoTracking()
            .Where(ut => ut.TeamId == team.Id)
            .Select(ut => ut.UserId)
            .ToListAsync(cancellationToken);

        var result = new List<Guid>();
        foreach (var uidStr in memberUserIds)
        {
            if (!Guid.TryParse(uidStr, out var uid))
                continue;

            var user = await userManager.FindByIdAsync(uidStr);
            if (user is null || !user.IsActive)
                continue;

            if (!await userManager.IsInRoleAsync(user, "Agent"))
                continue;

            result.Add(uid);
        }

        if (team.AutoAssignIgnoreOfflineUsers)
        {
            try
            {
                var presence = await presenceService.ListTenantPresenceAsync(tenantIdentifier, cancellationToken);
                var online = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in presence)
                {
                    if (p.IsOnline)
                        online.Add(p.UserId);
                }

                result = result.Where(u => online.Contains(u.ToString("D"))).ToList();
            }
            catch (RedisException ex)
            {
                logger.LogWarning(
                    ex,
                    "Redis presence unavailable; assigning without online filter. TeamId {TeamId} InboxId {InboxId}",
                    team.Id,
                    inboxId);
            }
        }

        if (team.AutoAssignMaxConversationsPerUser is { } cap && cap > 0)
        {
            var loadRows = await db.Conversations
                .AsNoTracking()
                .Where(c =>
                    c.InboxId == inboxId &&
                    c.Status == ConversationStatus.Open &&
                    c.AssignedAgentId != null)
                .GroupBy(c => c.AssignedAgentId!.Value)
                .Select(g => new { AgentId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var load = loadRows.ToDictionary(x => x.AgentId, x => x.Count);
            result = result.Where(u => !load.TryGetValue(u, out var c) || c < cap).ToList();
        }

        if (excludeUserId.HasValue)
            result = result.Where(u => u != excludeUserId.Value).ToList();

        return result.OrderBy(u => u).ToList();
    }

    private async Task<Guid?> PickRoundRobinAsync(Guid teamId, IReadOnlyList<Guid> candidates, CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
            return null;

        var idx = await roundRobinStore.GetNextSlotIndexAsync(teamId, candidates.Count, cancellationToken);
        if (idx < 0 || idx >= candidates.Count)
            idx = 0;

        return candidates[idx];
    }

    private async Task<Guid?> PickLeastAssignedAsync(Guid inboxId, IReadOnlyList<Guid> candidates, CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
            return null;

        var loadRows = await db.Conversations
            .AsNoTracking()
            .Where(c =>
                c.InboxId == inboxId &&
                c.Status == ConversationStatus.Open &&
                c.AssignedAgentId != null)
            .GroupBy(c => c.AssignedAgentId!.Value)
            .Select(g => new { AgentId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var load = loadRows.ToDictionary(x => x.AgentId, x => x.Count);

        return candidates
            .OrderBy(u => load.GetValueOrDefault(u, 0))
            .ThenBy(u => u)
            .First();
    }

    private async Task EnsureAgentParticipantAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var uid = userId.ToString("D");
        var existing = await db.ConversationParticipants
            .Where(p => p.ConversationId == conversationId && p.UserId == uid && p.Role == ConversationParticipantRole.Agent)
            .SingleOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            existing.IsActive = true;
            existing.LeftAt = null;
            return;
        }

        db.ConversationParticipants.Add(new ConversationParticipant
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            UserId = uid,
            ContactId = null,
            Role = ConversationParticipantRole.Agent,
            JoinedAt = DateTimeOffset.UtcNow,
            LeftAt = null,
            IsActive = true
        });
    }
}
