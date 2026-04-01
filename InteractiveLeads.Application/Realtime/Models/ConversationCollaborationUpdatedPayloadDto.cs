namespace InteractiveLeads.Application.Realtime.Models;

/// <summary>
/// Broadcast when assignment, transfer, or participant list changes so chat clients update without refresh.
/// </summary>
public sealed class ConversationCollaborationUpdatedPayloadDto
{
    public Guid Id { get; set; }
    public Guid InboxId { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public string? AssignedAgentName { get; set; }

    /// <summary>Active internal agent participants (user id string, "D" format) so clients can mirror server access rules in real time.</summary>
    public IReadOnlyList<string> ParticipantAgentUserIds { get; set; } = Array.Empty<string>();
}
