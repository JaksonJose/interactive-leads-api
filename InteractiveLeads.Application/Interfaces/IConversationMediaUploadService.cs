using InteractiveLeads.Application.Feature.Chat.Media;

namespace InteractiveLeads.Application.Interfaces;

public interface IConversationMediaUploadService
{
    Task<ConversationMediaUploadResultDto> UploadAsync(
        UploadConversationMediaRequest request,
        CancellationToken cancellationToken);
}
