using InteractiveLeads.Application.Feature.Chat.Messages;

namespace InteractiveLeads.Application.Realtime.Models;

public sealed class MessageUpdatedPayloadDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string MediaProcessingStatus { get; set; } = "processing";
    public MessageMediaListItemDto? Media { get; set; }
}
