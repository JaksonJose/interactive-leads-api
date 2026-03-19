using InteractiveLeads.Application.Responses;

namespace InteractiveLeads.Application.Interfaces.HttpRequests
{
    public interface IExternalApiHttpClient
    {
        Task<BaseResponse> GetAsync<T>(string uri, object? queryParams = null);

        Task<BaseResponse> PostAsync<T>(string uri, object data, object? queryParams = null);

        Task<BaseResponse> PostAsync(string uri, object data, object? queryParams = null);

        Task<BaseResponse> PutAsync<T>(string uri, object data, object? queryParams = null);

        Task<BaseResponse> PutAsync(string uri, object data, object? queryParams = null);

        Task<BaseResponse> DeleteAsync<T>(string uri);

        Task<BaseResponse> DeleteAsync(string uri);
    }
}
