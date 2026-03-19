using InteractiveLeads.Application.Interfaces.HttpRequests;
using Microsoft.Extensions.Configuration;

namespace InteractiveLeads.Infrastructure.HttpRequests;

public sealed class ExternalApiHttpClientFactory(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    IResponseHandlerProvider responseHandlerProvider) : IExternalApiHttpClientFactory
{
    public IExternalApiHttpClient Create(string apiName)
    {
        var handler = responseHandlerProvider.GetHandler(apiName);
        return new ExternalApiHttpClient(apiName, configuration, httpClientFactory, handler);
    }
}
