using InteractiveLeads.Application.Feature.Chat.Messages;

namespace InteractiveLeads.Application.Interfaces;

public interface IMessageService
{
    Task<MessageListItemDto> SendConversationMessageAsync(
        Guid conversationId,
        SendConversationMessageRequest request,
        CancellationToken cancellationToken);
}
