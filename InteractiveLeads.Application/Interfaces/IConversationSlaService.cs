namespace InteractiveLeads.Application.Interfaces;

/// <summary>Computes SLA deadlines on conversations from team/inbox policy; records first agent response.</summary>
public interface IConversationSlaService
{
    /// <param name="slaAnchorUtc">When set, both deadlines use this instant unless <paramref name="firstResponseAnchorUtc"/> / <paramref name="resolutionAnchorUtc"/> override.</param>
    /// <param name="firstResponseAnchorUtc">Optional anchor for first-response deadline only (e.g. last customer message after inactivity reassign).</param>
    /// <param name="resolutionAnchorUtc">Optional anchor for resolution deadline only (defaults to <paramref name="slaAnchorUtc"/> or creation time).</param>
    Task ApplySlaDeadlinesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default,
        DateTimeOffset? slaAnchorUtc = null,
        DateTimeOffset? firstResponseAnchorUtc = null,
        DateTimeOffset? resolutionAnchorUtc = null);

    /// <summary>Sets FirstAgentResponseAt once on the conversation when the first qualifying outbound is recorded.</summary>
    Task TryRecordFirstAgentResponseAsync(Guid conversationId, DateTimeOffset at, CancellationToken cancellationToken = default);
}
