using InteractiveLeads.Application.Feature.Chat.Messages.Outbound;
using InteractiveLeads.Application.Responses;

namespace InteractiveLeads.Application.Interfaces;

public interface IN8nClient
{
    Task<BaseResponse> SendMessageAsync(OutboundMessageContract payload, CancellationToken cancellationToken);
}
