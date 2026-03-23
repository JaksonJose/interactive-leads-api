using InteractiveLeads.Domain.Enums;

namespace InteractiveLeads.Application.Feature.Chat.Messages;

public static class MessageListItemDtoMapper
{
    public static string ToStatusString(MessageStatus status) =>
        status switch
        {
            MessageStatus.Pending => "pending",
            MessageStatus.Sent => "sent",
            MessageStatus.Delivered => "delivered",
            MessageStatus.Read => "read",
            MessageStatus.Failed => "failed",
            _ => "pending"
        };
}
