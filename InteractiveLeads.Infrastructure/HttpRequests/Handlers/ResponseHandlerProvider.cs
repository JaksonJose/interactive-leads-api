using InteractiveLeads.Application.Interfaces.HttpRequests;

namespace InteractiveLeads.Infrastructure.HttpRequests.Handlers;

public sealed class ResponseHandlerProvider(IEnumerable<IResponseHandler> handlers) : IResponseHandlerProvider
{
    private readonly IReadOnlyList<IResponseHandler> _handlers = handlers.ToList();

    public IResponseHandler GetHandler(string apiName)
    {
        var handler = _handlers.FirstOrDefault(x => x.CanHandle(apiName));
        if (handler == null)
            throw new InvalidOperationException($"No response handler registered for API '{apiName}'.");
        return handler;
    }
}
