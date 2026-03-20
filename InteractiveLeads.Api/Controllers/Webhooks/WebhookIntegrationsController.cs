using InteractiveLeads.Application.Feature.Integrations.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Webhooks;

[ApiController]
[Route("api/webhooks")]
public sealed class WebhookIntegrationsController : ControllerBase
{
    private readonly ISender _sender;

    public WebhookIntegrationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("integrations")]
    [OpenApiOperation("Get integration settings by external identifier (for webhook consumers)")]
    public async Task<IActionResult> GetIntegrationByIdentifierAsync(
        [FromQuery] string? provider,
        [FromQuery] string? identifier,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return BadRequest("Query parameter 'provider' is required.");

        if (string.IsNullOrWhiteSpace(identifier))
            return BadRequest("Query parameter 'identifier' is required.");

        var response = await _sender.Send(
            new GetIntegrationByIdentifierQuery { Provider = provider.Trim(), Identifier = identifier.Trim() },
            cancellationToken);

        return Ok(response);
    }
}

