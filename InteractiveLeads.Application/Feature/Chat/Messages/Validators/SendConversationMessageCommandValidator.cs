using FluentValidation;
using InteractiveLeads.Application.Feature.Chat.Messages.Commands;

namespace InteractiveLeads.Application.Feature.Chat.Messages.Validators;

public sealed class SendConversationMessageCommandValidator : AbstractValidator<SendConversationMessageCommand>
{
    private static readonly string[] SupportedTypes =
        ["text", "image", "video", "audio", "document", "reaction", "reply", "template"];

    public SendConversationMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("chat.message.conversation_required:ConversationId is required.");

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("chat.message.type_required:Message type is required.")
            .Must(type => SupportedTypes.Contains((type ?? string.Empty).Trim().ToLowerInvariant()))
            .WithMessage("chat.message.type_invalid:Message type is not supported.");

        When(x => IsType(x.Type, "text") || IsType(x.Type, "reply"), () =>
        {
            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("chat.message.content_required:Content is required.");
        });

        When(x => IsType(x.Type, "image") || IsType(x.Type, "video") || IsType(x.Type, "audio"), () =>
        {
            RuleFor(x => x.MediaUrl)
                .NotEmpty()
                .WithMessage("chat.message.media_url_required:Media URL is required.");
        });

        When(x => IsType(x.Type, "document"), () =>
        {
            RuleFor(x => x.MediaUrl)
                .NotEmpty()
                .WithMessage("chat.message.media_url_required:Media URL is required.");

            RuleFor(x => x.FileName)
                .NotEmpty()
                .WithMessage("chat.message.file_name_required:File name is required for documents.");
        });

        When(x => IsType(x.Type, "reaction"), () =>
        {
            RuleFor(x => x.ReactionEmoji)
                .NotEmpty()
                .WithMessage("chat.message.reaction_emoji_required:Reaction emoji is required.");

            RuleFor(x => x.ReactionMessageId)
                .NotNull()
                .WithMessage("chat.message.reaction_message_id_required:Reaction messageId is required.");
        });

        When(x => IsType(x.Type, "reply"), () =>
        {
            RuleFor(x => x.ReplyToMessageId)
                .NotNull()
                .WithMessage("chat.message.reply_to_required:Reply target messageId is required.");
        });

        When(x => IsType(x.Type, "template"), () =>
        {
            RuleFor(x => x.TemplateId)
                .NotNull()
                .WithMessage("chat.message.template_id_required:TemplateId is required for template messages.");
        });
    }

    private static bool IsType(string? source, string expected)
    {
        return string.Equals(source?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }
}
