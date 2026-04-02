using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ContactId { get; set; }
    public Guid IntegrationId { get; set; }
    public Guid InboxId { get; set; }

    /// <summary>Team whose SLA / routing rules apply after routing assigns this conversation.</summary>
    public Guid? HandlingTeamId { get; set; }

    /// <summary>SLA policy snapshot used when deadlines were last computed.</summary>
    public Guid? EffectiveSlaPolicyId { get; set; }

    /// <summary>Deadline for first agent response (wall-clock MVP, from <see cref="CreatedAt"/>).</summary>
    public DateTimeOffset? FirstResponseDueAt { get; set; }

    /// <summary>Deadline for resolution (wall-clock MVP, from <see cref="CreatedAt"/>).</summary>
    public DateTimeOffset? ResolutionDueAt { get; set; }

    /// <summary>When the first outbound agent message was recorded.</summary>
    public DateTimeOffset? FirstAgentResponseAt { get; set; }

    public ConversationStatus Status { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public DateTimeOffset? AssignedAt { get; set; }
    public int Priority { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public DateTimeOffset LastMessageAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Company Company { get; set; } = default!;
    public Contact Contact { get; set; } = default!;
    public Integration Integration { get; set; } = default!;
    public Inbox Inbox { get; set; } = default!;
    public Team? HandlingTeam { get; set; }

    public SlaPolicy? EffectiveSlaPolicy { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<ConversationAssignment> Assignments { get; set; } = new List<ConversationAssignment>();
}

