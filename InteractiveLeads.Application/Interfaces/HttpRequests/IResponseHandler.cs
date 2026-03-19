using InteractiveLeads.Application.Responses;

namespace InteractiveLeads.Application.Interfaces.HttpRequests;

public interface IResponseHandler
{
    bool CanHandle(string apiName);

    Task<BaseResponse> HandleAsync<T>(RawHttpResult rawResult);

    LoginResult? ExtractLoginResult(string content);
}
