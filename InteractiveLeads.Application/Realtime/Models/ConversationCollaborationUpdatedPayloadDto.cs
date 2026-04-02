using InteractiveLeads.Domain.Enums;

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

    /// <summary>Current conversation status (Open/Closed/Pending).</summary>
    public ConversationStatus Status { get; set; }

    public Guid? EffectiveSlaPolicyId { get; set; }
    public DateTimeOffset? FirstResponseDueAt { get; set; }
    public DateTimeOffset? ResolutionDueAt { get; set; }
    public DateTimeOffset? FirstAgentResponseAt { get; set; }

    public bool FirstResponseBreached { get; set; }
    public bool ResolutionBreached { get; set; }

    public bool LastMessageFromCustomer { get; set; }
    public int? CustomerInactivityReassignTimeoutMinutes { get; set; }
}
