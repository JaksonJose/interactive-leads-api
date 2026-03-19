using InteractiveLeads.Application.Interfaces.HttpRequests;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace InteractiveLeads.Infrastructure.HttpRequests.Authentications
{
    public sealed class BearerLoginAuthHandler : DelegatingHandler
    {
        private const int ExpirationMarginSeconds = 60;

        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IResponseHandlerProvider _responseHandlerProvider;
        private readonly string _apiName;
        private readonly JsonSerializerOptions _jsonOptions;
        private string _cachedToken;
        private DateTimeOffset? _cachedExpiresAt;
        private readonly object _tokenLock = new();

        public BearerLoginAuthHandler(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IResponseHandlerProvider responseHandlerProvider,
            string apiName)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _responseHandlerProvider = responseHandlerProvider ?? throw new ArgumentNullException(nameof(responseHandlerProvider));
            _apiName = apiName ?? throw new ArgumentNullException(nameof(apiName));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await GetTokenAsync(cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
        {
            lock (_tokenLock)
            {
                if (!string.IsNullOrEmpty(_cachedToken) && IsTokenStillValid())
                    return _cachedToken;
            }

            var baseUrl = _configuration[$"Integration:{_apiName}:Url"];
            var username = _configuration[$"Integration:{_apiName}:Username"];
            var password = _configuration[$"Integration:{_apiName}:Password"];

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                throw new HttpRequestException($"Incomplete login configuration for API {_apiName}.");

            var loginClientName = $"{_apiName}.Login";
            var loginClient = _httpClientFactory.CreateClient(loginClientName);

            var loginPath = _configuration[$"Integration:{_apiName}:LoginPath"];
            if (string.IsNullOrWhiteSpace(loginPath))
                loginPath = "/login";
            else
                loginPath = "/" + loginPath.TrimStart('/');

            var body = new { username, password };

            try
            {
                var response = await loginClient.PostAsJsonAsync(loginPath, body, _jsonOptions, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Login failed for API {_apiName}: {response.StatusCode} ({(int)response.StatusCode}).");

                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var handler = _responseHandlerProvider.GetHandler(_apiName);
                var result = handler.ExtractLoginResult(content);

                if (result == null || string.IsNullOrEmpty(result.Token))
                    throw new HttpRequestException($"Login response from API {_apiName} does not contain a token.");

                var expiresAt = result.ExpiresAt;
                if (expiresAt == null && LooksLikeJwt(result.Token))
                    expiresAt = GetJwtExpiration(result.Token);

                lock (_tokenLock)
                {
                    _cachedToken = result.Token;
                    _cachedExpiresAt = expiresAt;
                }

                return result.Token;
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Failed to obtain authentication token for API {_apiName}: {ex.Message}", ex);
            }
        }

        private bool IsTokenStillValid()
        {
            if (_cachedExpiresAt == null)
                return true;
            return DateTimeOffset.UtcNow < _cachedExpiresAt.Value.AddSeconds(-ExpirationMarginSeconds);
        }

        private static bool LooksLikeJwt(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            var parts = token.Split('.');
            return parts.Length == 3;
        }

        private static DateTimeOffset? GetJwtExpiration(string jwt)
        {
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length != 3) return null;

                var payload = parts[1];
                var base64 = payload.Replace('-', '+').Replace('_', '/');
                switch (base64.Length % 4)
                {
                    case 2: base64 += "=="; break;
                    case 3: base64 += "="; break;
                }
                var bytes = Convert.FromBase64String(base64);
                var json = Encoding.UTF8.GetString(bytes);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("exp", out var expEl) && expEl.ValueKind == JsonValueKind.Number && expEl.TryGetInt64(out var exp))
                    return DateTimeOffset.FromUnixTimeSeconds(exp);
            }
            catch (Exception ex)
            {
                throw new HttpRequestException("Could not read JWT token expiration (malformed or invalid token).", ex);
            }
            return null;
        }
    }
}
