using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Chat;

public sealed class ConversationSlaService(IApplicationDbContext db) : IConversationSlaService
{
    public async Task ApplySlaDeadlinesAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conv = await db.Conversations
            .Include(c => c.Inbox)
            .Include(c => c.HandlingTeam)
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conv is null)
            return;

        var policyId = await ResolveEffectiveSlaPolicyIdAsync(conv, cancellationToken);

        if (!policyId.HasValue)
        {
            conv.EffectiveSlaPolicyId = null;
            conv.FirstResponseDueAt = null;
            conv.ResolutionDueAt = null;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var policy = await db.SlaPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == policyId.Value && p.CompanyId == conv.CompanyId && p.IsActive,
                cancellationToken);

        if (policy is null)
        {
            conv.EffectiveSlaPolicyId = null;
            conv.FirstResponseDueAt = null;
            conv.ResolutionDueAt = null;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        conv.EffectiveSlaPolicyId = policy.Id;
        var anchor = conv.CreatedAt;
        conv.FirstResponseDueAt = anchor.AddMinutes(policy.FirstResponseTargetMinutes);
        conv.ResolutionDueAt = anchor.AddMinutes(policy.ResolutionTargetMinutes);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 1) Team that is handling the conversation (if any): its SLA, else inbox default.
    /// 2) No handling team: inbox default SLA.
    /// 3) Still none: first inbox-linked team (by <see cref="InboxTeam.Priority"/>) that defines <see cref="Team.SlaPolicyId"/>.
    /// </summary>
    private async Task<Guid?> ResolveEffectiveSlaPolicyIdAsync(Conversation conv, CancellationToken cancellationToken)
    {
        if (conv.HandlingTeamId.HasValue && conv.HandlingTeam is not null)
        {
            if (conv.HandlingTeam.SlaPolicyId.HasValue)
                return conv.HandlingTeam.SlaPolicyId;

            return conv.Inbox.DefaultSlaPolicyId;
        }

        if (conv.Inbox.DefaultSlaPolicyId.HasValue)
            return conv.Inbox.DefaultSlaPolicyId;

        return await GetFirstLinkedTeamSlaPolicyIdAsync(conv.InboxId, conv.CompanyId, cancellationToken);
    }

    private async Task<Guid?> GetFirstLinkedTeamSlaPolicyIdAsync(
        Guid inboxId,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var teamIds = await db.InboxTeams
            .AsNoTracking()
            .Where(l => l.InboxId == inboxId)
            .OrderBy(l => l.Priority)
            .Select(l => l.TeamId)
            .ToListAsync(cancellationToken);

        foreach (var teamId in teamIds)
        {
            var policyId = await db.Teams
                .AsNoTracking()
                .Where(t => t.Id == teamId && t.CompanyId == companyId && t.IsActive)
                .Select(t => t.SlaPolicyId)
                .FirstOrDefaultAsync(cancellationToken);

            if (policyId.HasValue)
                return policyId;
        }

        return null;
    }

    public async Task TryRecordFirstAgentResponseAsync(
        Guid conversationId,
        DateTimeOffset at,
        CancellationToken cancellationToken = default)
    {
        var conv = await db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conv is null || conv.FirstAgentResponseAt.HasValue)
            return;

        conv.FirstAgentResponseAt = at;
        await db.SaveChangesAsync(cancellationToken);
    }
}
