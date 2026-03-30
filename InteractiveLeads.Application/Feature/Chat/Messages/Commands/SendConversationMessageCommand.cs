using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Feature.Chat.Messages;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;

namespace InteractiveLeads.Application.Feature.Chat.Messages.Commands;

public sealed class SendConversationMessageCommand : IApplicationRequest<IResponse>
{
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public long? ClientTimestamp { get; set; }
    public string? ExternalMessageId { get; set; }
    public string Type { get; set; } = "text";
    public string? MediaUrl { get; set; }
    public string? Caption { get; set; }
    public string? MimeType { get; set; }
    public string? FileName { get; set; }
    public string? MediaOptimizedUrl { get; set; }
    public string? MediaOptimizedMimeType { get; set; }
    public string? MediaOptimizedFileName { get; set; }
    public string? MediaThumbnailUrl { get; set; }
    public bool? Voice { get; set; }
    public string? ReactionEmoji { get; set; }
    public Guid? ReactionMessageId { get; set; }
    public Guid? ReplyToMessageId { get; set; }

    /// <summary>WhatsApp template (CRM) id to send when <see cref="Type"/> is <c>template</c>.</summary>
    public Guid? TemplateId { get; set; }

    /// <summary>Template variables for BODY, in order {{1}}, {{2}}, ...</summary>
    public string[]? TemplateBodyParameters { get; set; }

    /// <summary>Optional single variable for HEADER when the template uses a text header.</summary>
    public string? TemplateHeaderParameter { get; set; }
}

public sealed class SendConversationMessageCommandHandler(
    IMessageService messageService) : IApplicationRequestHandler<SendConversationMessageCommand, IResponse>
{
    public async Task<IResponse> Handle(SendConversationMessageCommand request, CancellationToken cancellationToken)
    {
        var sentMessage = await messageService.SendConversationMessageAsync(
            request.ConversationId,
            new SendConversationMessageRequest
            {
                Content = request.Content,
                ClientTimestamp = request.ClientTimestamp,
                ExternalMessageId = request.ExternalMessageId,
                Type = request.Type,
                MediaUrl = request.MediaUrl,
                Caption = request.Caption,
                MimeType = request.MimeType,
                FileName = request.FileName,
                MediaOptimizedUrl = request.MediaOptimizedUrl,
                MediaOptimizedMimeType = request.MediaOptimizedMimeType,
                MediaOptimizedFileName = request.MediaOptimizedFileName,
                MediaThumbnailUrl = request.MediaThumbnailUrl,
                Voice = request.Voice,
                ReactionEmoji = request.ReactionEmoji,
                ReactionMessageId = request.ReactionMessageId,
                ReplyToMessageId = request.ReplyToMessageId,
                TemplateId = request.TemplateId,
                TemplateBodyParameters = request.TemplateBodyParameters,
                TemplateHeaderParameter = request.TemplateHeaderParameter
            },
            cancellationToken);

        return new SingleResponse<MessageListItemDto>(sentMessage);
    }
}

