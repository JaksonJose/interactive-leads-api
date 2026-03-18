namespace InteractiveLeads.Application.Realtime.Models;

public class ConversationMovedToTopPayloadDto
{
    public Guid Id { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public DateTimeOffset LastMessageAt { get; set; }
}

