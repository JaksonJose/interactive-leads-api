using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Feature.Chat.Messages;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Feature.Chat.Messages.Commands;

public sealed class SendConversationMessageCommand : IRequest<IResponse>
{
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ExternalMessageId { get; set; }
    public string Type { get; set; } = "text";
    public string? MediaUrl { get; set; }
    public string? Caption { get; set; }
    public string? ReactionEmoji { get; set; }
    public Guid? ReactionMessageId { get; set; }
    public Guid? ReplyToMessageId { get; set; }
}

public sealed class SendConversationMessageCommandHandler(
    IMessageService messageService) : IRequestHandler<SendConversationMessageCommand, IResponse>
{
    public async Task<IResponse> Handle(SendConversationMessageCommand request, CancellationToken cancellationToken)
    {
        var sentMessage = await messageService.SendConversationMessageAsync(
            request.ConversationId,
            new SendConversationMessageRequest
            {
                Content = request.Content,
                ExternalMessageId = request.ExternalMessageId,
                Type = request.Type,
                MediaUrl = request.MediaUrl,
                Caption = request.Caption,
                ReactionEmoji = request.ReactionEmoji,
                ReactionMessageId = request.ReactionMessageId,
                ReplyToMessageId = request.ReplyToMessageId
            },
            cancellationToken);

        return new SingleResponse<MessageListItemDto>(sentMessage);
    }
}
