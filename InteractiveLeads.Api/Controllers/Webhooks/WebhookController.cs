using InteractiveLeads.Application.Feature.Webhooks.Messages;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Webhooks;

[ApiController]
[Route("api/webhooks")]
public sealed class WebhookController(ISender sender) : ControllerBase
{
    [HttpPost("messages")]
    [OpenApiOperation("Public webhook used to process normalized provider events (n8n).")]
    public async Task<IActionResult> ReceiveMessageAsync([FromBody] WebhookEventRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest();

        if (string.IsNullOrWhiteSpace(request.Provider) ||
            string.IsNullOrWhiteSpace(request.EventType) ||
            request.Identifications == null ||
            string.IsNullOrWhiteSpace(request.Identifications.ExternalIdentifier) ||
            request.Payload.ValueKind == System.Text.Json.JsonValueKind.Undefined)
        {
            return BadRequest("Invalid payload.");
        }

        var response = await sender.Send(new ProcessWebhookEventCommand { Event = request }, cancellationToken);
        return Ok(response);
    }
}
