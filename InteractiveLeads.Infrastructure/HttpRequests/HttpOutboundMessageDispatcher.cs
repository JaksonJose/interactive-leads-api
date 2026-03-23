using System.Text;
using System.Text.Json;
using InteractiveLeads.Application.Feature.Chat.Messages.Outbound;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.Extensions.Logging;

namespace InteractiveLeads.Infrastructure.HttpRequests;

/// <summary>POSTs outbound payloads to the configured Integration:MessageSender HTTP base URL.</summary>
public sealed class HttpOutboundMessageDispatcher(
    IHttpClientFactory httpClientFactory,
    ILogger<HttpOutboundMessageDispatcher> logger) : IOutboundMessageDispatcher
{
    private const string HttpClientName = "MessageSender";
    private const string SendPath = "webhook-test/send-message";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<OutboundDispatchOutcome> SendMessageAsync(OutboundMessageContract payload, CancellationToken cancellationToken)
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
                return new OutboundDispatchOutcome { Response = response };
            }

            var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "HTTP outbound dispatch failed with status {StatusCode}, provider {Provider}, channel {ChannelId}, messageId {MessageId}, body {Body}",
                (int)httpResponse.StatusCode,
                payload.Provider,
                payload.ChannelId,
                payload.Payload.Id,
                body);

            response.AddErrorMessage("External HTTP channel rejected outbound message.", "chat.message.outbound_http_rejected");
            return new OutboundDispatchOutcome { Response = response };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "HTTP outbound dispatch threw for provider {Provider}, channel {ChannelId}, messageId {MessageId}",
                payload.Provider,
                payload.ChannelId,
                payload.Payload.Id);
            response.AddExceptionMessage("Error while dispatching outbound message over HTTP.", "chat.message.outbound_http_dispatch_error");
            return new OutboundDispatchOutcome { Response = response };
        }
    }
}
