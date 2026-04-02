using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Feature.Chat.Conversations;

public sealed class InboxConversationListItemDto
{
    public Guid Id { get; set; }
    public Guid InboxId { get; set; }
    public Guid ContactId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTimeOffset LastMessageAt { get; set; }

    /// <summary>True when the last message in the thread is from the customer (used with inactivity reassign).</summary>
    public bool LastMessageFromCustomer { get; set; }

    /// <summary>
    /// When set, minutes after <see cref="LastMessageAt"/> before inactivity auto-reassign (team routing).
    /// </summary>
    public int? CustomerInactivityReassignTimeoutMinutes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public string InboxName { get; set; } = string.Empty;
    public ConversationStatus Status { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public string? AssignedAgentName { get; set; }

    public Guid? EffectiveSlaPolicyId { get; set; }
    public DateTimeOffset? FirstResponseDueAt { get; set; }
    public DateTimeOffset? ResolutionDueAt { get; set; }
    public DateTimeOffset? FirstAgentResponseAt { get; set; }

    /// <summary>True when first-response deadline passed with no agent reply yet.</summary>
    public bool FirstResponseBreached { get; set; }

    /// <summary>True when resolution deadline passed while conversation is not closed.</summary>
    public bool ResolutionBreached { get; set; }

    public void ApplySlaBreachFlags(DateTimeOffset utcNow)
    {
        FirstResponseBreached = FirstResponseDueAt.HasValue
            && !FirstAgentResponseAt.HasValue
            && utcNow > FirstResponseDueAt.Value;

        ResolutionBreached = ResolutionDueAt.HasValue
            && Status != ConversationStatus.Closed
            && utcNow > ResolutionDueAt.Value;
    }
}

