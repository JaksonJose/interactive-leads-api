using System.Text;
using System.Text.Json;
using InteractiveLeads.Application.Feature.Chat.Messages.Outbound;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.Extensions.Logging;

namespace InteractiveLeads.Infrastructure.HttpRequests;

public sealed class N8nHttpClient(
    IHttpClientFactory httpClientFactory,
    ILogger<N8nHttpClient> logger) : IN8nClient
{
    private const string HttpClientName = "MessageSender";
    private const string SendPath = "webhook-test/send-message";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<BaseResponse> SendMessageAsync(OutboundMessageContract payload, CancellationToken cancellationToken)
    {
        var response = new BaseResponse();
        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, SendPath)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload, JsonOptions),
                    Encoding.UTF8,
                    "application/json")
            };

            using var httpResponse = await client.SendAsync(request, cancellationToken);
            if (httpResponse.IsSuccessStatusCode)
            {
                return response;
            }

            var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "n8n outbound dispatch failed with status {StatusCode}, provider {Provider}, channel {ChannelId}, messageId {MessageId}, body {Body}",
                (int)httpResponse.StatusCode,
                payload.Provider,
                payload.ChannelId,
                payload.Message.Id,
                body);

            response.AddErrorMessage("n8n rejected outbound message.", "chat.message.n8n_rejected");
            return response;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "n8n outbound dispatch threw an exception for provider {Provider}, channel {ChannelId}, messageId {MessageId}",
                payload.Provider,
                payload.ChannelId,
                payload.Message.Id);
            response.AddExceptionMessage("Error while dispatching outbound message to n8n.", "chat.message.n8n_dispatch_error");
            return response;
        }
    }
}
