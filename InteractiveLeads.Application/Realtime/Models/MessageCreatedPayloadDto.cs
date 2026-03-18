namespace InteractiveLeads.Application.Realtime.Models;

public class MessageCreatedPayloadDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? SenderId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

