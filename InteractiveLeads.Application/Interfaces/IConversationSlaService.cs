namespace InteractiveLeads.Application.Interfaces;

/// <summary>Computes SLA deadlines on conversations from team/inbox policy; records first agent response.</summary>
public interface IConversationSlaService
{
    Task ApplySlaDeadlinesAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>Sets FirstAgentResponseAt once on the conversation when the first qualifying outbound is recorded.</summary>
    Task TryRecordFirstAgentResponseAsync(Guid conversationId, DateTimeOffset at, CancellationToken cancellationToken = default);
}
