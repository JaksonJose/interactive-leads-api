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
    public async Task PublishCollaborationUpdatedAsync(Conversation conversation, CancellationToken cancellationToken)
    {
        var tenantId = currentUserService.GetUserTenant();
        if (string.IsNullOrWhiteSpace(tenantId))
            return;

        string? assignedName = null;
        if (conversation.AssignedAgentId.HasValue)
        {
            var key = conversation.AssignedAgentId.Value.ToString("D");
            var summaries = await userSummaryLookup.GetSummariesByIdsAsync(new[] { key }, cancellationToken);
            if (summaries.TryGetValue(key, out var s))
                assignedName = s.DisplayName;
        }

        var participantIds = await db.ConversationParticipants
            .AsNoTracking()
            .Where(p =>
                p.ConversationId == conversation.Id &&
                p.Role == ConversationParticipantRole.Agent &&
                p.IsActive)
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken);

        var payload = new ConversationCollaborationUpdatedPayloadDto
        {
            Id = conversation.Id,
            InboxId = conversation.InboxId,
            AssignedAgentId = conversation.AssignedAgentId,
            AssignedAgentName = assignedName,
            ParticipantAgentUserIds = participantIds
        };

        var evt = new RealtimeEvent<ConversationCollaborationUpdatedPayloadDto>
        {
            Type = "conversation.collaboration_updated",
            TenantId = tenantId,
            Timestamp = DateTime.UtcNow,
            Payload = payload
        };

        // Broadcast to the whole tenant (same group as presence) so every agent receives updates
        // even if JoinInbox/JoinConversation failed silently or the user was just granted access.
        await realtimeService.SendToTenantAsync(tenantId, evt);
    }
}
