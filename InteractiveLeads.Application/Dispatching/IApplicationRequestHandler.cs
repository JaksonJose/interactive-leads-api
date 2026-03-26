using System.Threading;

namespace InteractiveLeads.Application.Dispatching;

public interface IApplicationRequestHandler<in TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
    where TResponse : class
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

