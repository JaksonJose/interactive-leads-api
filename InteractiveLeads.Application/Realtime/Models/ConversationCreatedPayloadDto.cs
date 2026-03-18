namespace InteractiveLeads.Application.Realtime.Models;

public class ConversationCreatedPayloadDto
{
    public Guid Id { get; set; }
    public Guid InboxId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

