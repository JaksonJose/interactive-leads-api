using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Realtime.Models;
using InteractiveLeads.Application.Realtime.Services;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Conversations.Services;

public sealed class ConversationCollaborationRealtimePublisher(
    IApplicationDbContext db,
    IRealtimeService realtimeService,
    IUserSummaryLookupService userSummaryLookup,
    ICurrentUserService currentUserService) : IConversationCollaborationRealtimePublisher
{
    public Task PublishCollaborationUpdatedAsync(Conversation conversation, CancellationToken cancellationToken)
    {
        var tenantId = currentUserService.GetUserTenant();
        if (string.IsNullOrWhiteSpace(tenantId))
            return Task.CompletedTask;

        return PublishCollaborationUpdatedAsync(conversation, tenantId, cancellationToken);
    }

    public async Task PublishCollaborationUpdatedAsync(Conversation conversation, string tenantIdentifier, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
            return;

        // Reload so SLA fields and assignee match the database (handlers often call SaveChanges on a tracked graph, then
        // IConversationSlaService loads the row again — the passed entity can be stale).
        var conv = await db.Conversations
            .AsNoTracking()
            .Include(c => c.HandlingTeam)
            .FirstOrDefaultAsync(c => c.Id == conversation.Id, cancellationToken);

        if (conv is null)
            return;

        string? assignedName = null;
        if (conv.AssignedAgentId.HasValue)
        {
            var key = conv.AssignedAgentId.Value.ToString("D");
            var summaries = await userSummaryLookup.GetSummariesByIdsAsync(new[] { key }, cancellationToken);
            if (summaries.TryGetValue(key, out var s))
                assignedName = s.DisplayName;
        }

        var participantIds = (await db.ConversationParticipants
                .AsNoTracking()
                .Where(p =>
                    p.ConversationId == conv.Id &&
                    p.Role == ConversationParticipantRole.Agent &&
                    p.IsActive)
                .Select(p => p.UserId)
                .ToListAsync(cancellationToken))
            .Where(uid => !string.IsNullOrEmpty(uid))
            .Select(uid => uid!)
            .ToList();

        int? inactivityTimeout = null;
        var reassignOnFirstSla = false;
        if (conv.HandlingTeam is { AutoAssignEnabled: true })
        {
            if (conv.HandlingTeam.AutoAssignReassignTimeoutMinutes is > 0)
                inactivityTimeout = conv.HandlingTeam.AutoAssignReassignTimeoutMinutes;
            reassignOnFirstSla = conv.HandlingTeam.AutoReassignOnFirstResponseSlaExpired;
        }

        var utcNow = DateTimeOffset.UtcNow;
        var firstBreached = conv.FirstResponseDueAt.HasValue
                            && !conv.FirstAgentResponseAt.HasValue
                            && utcNow > conv.FirstResponseDueAt.Value;
        var resolutionBreached = conv.ResolutionDueAt.HasValue
                                 && conv.Status != ConversationStatus.Closed
                                 && utcNow > conv.ResolutionDueAt.Value;

        var payload = new ConversationCollaborationUpdatedPayloadDto
        {
            Id = conv.Id,
            InboxId = conv.InboxId,
            AssignedAgentId = conv.AssignedAgentId,
            AssignedAgentName = assignedName,
            ParticipantAgentUserIds = participantIds,
            Status = conv.Status,
            EffectiveSlaPolicyId = conv.EffectiveSlaPolicyId,
            FirstResponseDueAt = conv.FirstResponseDueAt,
            ResolutionDueAt = conv.ResolutionDueAt,
            FirstAgentResponseAt = conv.FirstAgentResponseAt,
            FirstResponseBreached = firstBreached,
            ResolutionBreached = resolutionBreached,
            LastMessageFromCustomer = conv.LastMessageFromCustomer,
            CustomerInactivityReassignTimeoutMinutes = inactivityTimeout,
            ReassignOnFirstResponseSlaExpired = reassignOnFirstSla
        };

        var evt = new RealtimeEvent<ConversationCollaborationUpdatedPayloadDto>
        {
            Type = "conversation.collaboration_updated",
            TenantId = tenantIdentifier,
            Timestamp = DateTime.UtcNow,
            Payload = payload
        };

        await realtimeService.SendToTenantAsync(tenantIdentifier, evt);
    }
}
