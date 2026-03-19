namespace InteractiveLeads.Application.Interfaces.HttpRequests;

public interface IResponseHandlerProvider
{
    IResponseHandler GetHandler(string apiName);
}
