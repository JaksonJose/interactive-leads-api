namespace InteractiveLeads.Application.Dispatching;

public interface IApplicationRequest<out TResponse>
    where TResponse : class
{
}

