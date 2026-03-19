using InteractiveLeads.Application.Interfaces.HttpRequests;
using InteractiveLeads.Application.Responses;
using Microsoft.Extensions.Configuration;
using Polly.CircuitBreaker;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InteractiveLeads.Infrastructure.HttpRequests
{
    public sealed class ExternalApiHttpClient : IExternalApiHttpClient
    {
        private readonly string _apiName;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IResponseHandler _responseHandler;

        public ExternalApiHttpClient(string apiName, IConfiguration configuration, IHttpClientFactory httpClientFactory, IResponseHandler responseHandler)
        {
            _apiName = apiName ?? throw new ArgumentNullException(nameof(apiName));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _responseHandler = responseHandler ?? throw new ArgumentNullException(nameof(responseHandler));
        }

        public Task<BaseResponse> GetAsync<T>(string uri, object? queryParams = null) => SendAsync<T>(method: HttpMethod.Get, uri: uri, queryParams: queryParams);

        public Task<BaseResponse> PostAsync<T>(string uri, object data, object? queryParams = null) => SendAsync<T>(HttpMethod.Post, uri, data, queryParams);

        public async Task<BaseResponse> PostAsync(string uri, object data, object? queryParams = null) => await PostAsync<object>(uri, data, queryParams);

        public Task<BaseResponse> PutAsync<T>(string uri, object data, object? queryParams = null) => SendAsync<T>(HttpMethod.Put, uri, data, queryParams);

        public async Task<BaseResponse> PutAsync(string uri, object data, object? queryParams = null) => await PutAsync<object>(uri, data, queryParams);

        public Task<BaseResponse> DeleteAsync<T>(string uri) => SendAsync<T>(HttpMethod.Delete, uri);

        public async Task<BaseResponse> DeleteAsync(string uri) => await DeleteAsync<object>(uri);

        private async Task<BaseResponse> SendAsync<T>(HttpMethod method, string uri, object? body = null, object? queryParams = null)
        {
            BaseResponse response = new();

            try
            {
                var baseUrl = _configuration[$"Integration:{_apiName}:Url"];
                if (string.IsNullOrEmpty(baseUrl))
                {
                    response.AddErrorMessage($"Missing Integration:{_apiName}:Url configuration.");
                    return response;
                }

                var rawResult = await SendRawAsync(method, uri, baseUrl, body, queryParams);
                response = await _responseHandler.HandleAsync<T>(rawResult);
            }
            catch (BrokenCircuitException)
            {
                response.AddExceptionMessage("Service is temporarily unavailable due to repeated failures. Please retry shortly.");
            }
            catch (HttpRequestException ex)
            {
                response.AddExceptionMessage(string.IsNullOrWhiteSpace(ex.Message)
                    ? "Service unavailable or host unreachable. Please retry later."
                    : ex.Message);
            }
            catch (TaskCanceledException)
            {
                response.AddExceptionMessage("Request timed out while contacting external API. Please retry later.");
            }
            catch (SocketException ex)
            {
                response.AddExceptionMessage($"Could not connect to remote server: {ex.Message}");
            }
            catch (Exception ex)
            {
                response.AddExceptionMessage($"Unexpected error while calling external API: {ex.Message}");
            }

            return response;
        }

        private async Task<RawHttpResult> SendRawAsync(HttpMethod method, string uri, string baseUrl, object? body, object? queryParams)
        {
            var client = _httpClientFactory.CreateClient(_apiName);

            var fullUri = BuildUri(baseUrl, uri, queryParams);
            var message = new HttpRequestMessage
            {
                RequestUri = fullUri,
                Method = method
            };

            message.Headers.Add("Accept", "application/json");

            if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put))
            {
                var jsonContent = JsonSerializer.Serialize(body);
                message.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }

            var apiResponse = await client.SendAsync(message);
            var apiContent = await apiResponse.Content.ReadAsStringAsync();

            return new RawHttpResult(apiContent, apiResponse.StatusCode);
        }

        private static Uri BuildUri(string baseUrl, string uri, object? queryParams)
        {
            var baseUri = new Uri(baseUrl.TrimEnd('/'));
            var path = uri.TrimStart('/');
            var fullPath = string.IsNullOrEmpty(path) ? baseUri.ToString() : new Uri(baseUri, path).ToString();

            var queryString = BuildQueryString(queryParams);
            if (!string.IsNullOrEmpty(queryString))
                fullPath += (fullPath.Contains("?") ? "&" : "?") + queryString;

            return new Uri(fullPath);
        }

        private static string BuildQueryString(object? queryParams)
        {
            if (queryParams == null)
                return string.Empty;

            if (queryParams is IEnumerable<KeyValuePair<string, string>> stringDict)
                return string.Join("&", stringDict.Where(kv => kv.Value != null).Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

            if (queryParams is IEnumerable<KeyValuePair<string, object>> objectDict)
                return string.Join("&", objectDict.Where(kv => kv.Value != null).Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(FormatQueryValue(kv.Value))}"));

            var pairs = new List<string>();
            var type = queryParams.GetType();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead);

            foreach (var prop in props)
            {
                var value = prop.GetValue(queryParams);
                if (value == null)
                    continue;

                var key = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name;
                var val = Uri.EscapeDataString(FormatQueryValue(value));
                pairs.Add($"{Uri.EscapeDataString(key)}={val}");
            }

            return string.Join("&", pairs);
        }

        private static string FormatQueryValue(object value)
        {
            return value switch
            {
                DateTime dt => dt.ToString("o"),
                DateTimeOffset dto => dto.ToString("o"),
                _ => value.ToString() ?? string.Empty
            };
        }
    }
}
