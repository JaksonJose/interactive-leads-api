using System.Threading;
using System.Threading.Tasks;

namespace InteractiveLeads.Application.Dispatching;

public interface IRequestDispatcher
{
    Task<TResponse> Send<TResponse>(
        IApplicationRequest<TResponse> request,
        CancellationToken cancellationToken = default)
        where TResponse : class;
}

