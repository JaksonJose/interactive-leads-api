using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ContactId { get; set; }
    public Guid IntegrationId { get; set; }
    public ConversationStatus Status { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public DateTimeOffset LastMessageAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Company Company { get; set; } = default!;
    public Contact Contact { get; set; } = default!;
    public Integration Integration { get; set; } = default!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
}

