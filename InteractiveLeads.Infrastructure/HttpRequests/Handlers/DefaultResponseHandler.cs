using System.Globalization;
using System.Net;
using System.Text.Json;
using InteractiveLeads.Application.Interfaces.HttpRequests;
using InteractiveLeads.Application.Responses;

namespace InteractiveLeads.Infrastructure.HttpRequests.Handlers;

public sealed class DefaultResponseHandler : IResponseHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public bool CanHandle(string apiName) => true;

    public Task<BaseResponse> HandleAsync<T>(RawHttpResult rawResult)
    {
        if (!IsSuccess(rawResult.StatusCode))
        {
            var errorResponse = new ResultResponse();
            var message = string.IsNullOrWhiteSpace(rawResult.Content)
                ? $"External API request failed with status {(int)rawResult.StatusCode} ({rawResult.StatusCode})."
                : rawResult.Content;
            errorResponse.AddErrorMessage(message, ((int)rawResult.StatusCode).ToString(CultureInfo.InvariantCulture));
            return Task.FromResult<BaseResponse>(errorResponse);
        }

        if (typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(rawResult.Content))
            return Task.FromResult<BaseResponse>(new ResultResponse());

        try
        {
            var data = JsonSerializer.Deserialize<T>(rawResult.Content, JsonOptions);
            if (data == null)
            {
                var nullBodyResponse = new ResultResponse();
                nullBodyResponse.AddErrorMessage("External API returned an empty payload.");
                return Task.FromResult<BaseResponse>(nullBodyResponse);
            }

            if (data is not object model)
            {
                var invalidDataResponse = new ResultResponse();
                invalidDataResponse.AddErrorMessage("External API returned an invalid payload.");
                return Task.FromResult<BaseResponse>(invalidDataResponse);
            }

            return Task.FromResult<BaseResponse>(new SingleResponse<object>(model));
        }
        catch (JsonException ex)
        {
            var parseResponse = new ResultResponse();
            parseResponse.AddErrorMessage($"Failed to parse external API response: {ex.Message}");
            return Task.FromResult<BaseResponse>(parseResponse);
        }
    }

    public LoginResult? ExtractLoginResult(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            string? token = ReadString(root, "token")
                ?? ReadString(root, "accessToken")
                ?? ReadString(root, "access_token");

            if (string.IsNullOrWhiteSpace(token))
            {
                if (root.TryGetProperty("data", out var dataElement))
                {
                    token = ReadString(dataElement, "token")
                        ?? ReadString(dataElement, "accessToken")
                        ?? ReadString(dataElement, "access_token");
                }
            }

            if (string.IsNullOrWhiteSpace(token))
                return null;

            DateTimeOffset? expiresAt = ReadDate(root, "expiresAt")
                ?? ReadDate(root, "expires_at")
                ?? ReadUnixTime(root, "expiresIn")
                ?? ReadUnixTime(root, "expires_in");

            return new LoginResult
            {
                Token = token,
                ExpiresAt = expiresAt
            };
        }
        catch
        {
            return null;
        }
    }

    private static bool IsSuccess(HttpStatusCode statusCode)
    {
        var value = (int)statusCode;
        return value is >= 200 and < 300;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
            return null;
        return value.GetString();
    }

    private static DateTimeOffset? ReadDate(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(value.GetString(), out var parsed))
            return parsed;
        return null;
    }

    private static DateTimeOffset? ReadUnixTime(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var seconds))
            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        return null;
    }
}
