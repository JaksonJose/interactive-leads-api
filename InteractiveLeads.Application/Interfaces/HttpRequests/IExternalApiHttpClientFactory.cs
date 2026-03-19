namespace InteractiveLeads.Application.Interfaces.HttpRequests;

public interface IExternalApiHttpClientFactory
{
    IExternalApiHttpClient Create(string apiName);
}
