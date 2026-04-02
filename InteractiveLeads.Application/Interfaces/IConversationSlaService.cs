namespace InteractiveLeads.Application.Interfaces;

/// <summary>Computes SLA deadlines on conversations from team/inbox policy; records first agent response.</summary>
public interface IConversationSlaService
{
    /// <param name="slaAnchorUtc">When set, first-response and resolution deadlines are computed from this instant; otherwise from conversation creation time.</param>
    Task ApplySlaDeadlinesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default,
        DateTimeOffset? slaAnchorUtc = null);

    /// <summary>Sets FirstAgentResponseAt once on the conversation when the first qualifying outbound is recorded.</summary>
    Task TryRecordFirstAgentResponseAsync(Guid conversationId, DateTimeOffset at, CancellationToken cancellationToken = default);
}
