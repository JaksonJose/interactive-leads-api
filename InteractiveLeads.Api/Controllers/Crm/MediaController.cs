using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.Inbound.Media;
using InteractiveLeads.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Crm;

[Authorize(Roles = "Owner,Manager,Agent")]
public sealed class MediaController(IMediaProcessor mediaProcessor) : BaseApiController
{
    /// <summary>
    /// Manual media processing endpoint. Useful for diagnostics and pipeline validation.
    /// </summary>
    [HttpPost("/api/v1/media/process")]
    [OpenApiOperation("Process media manually")]
    public async Task<IActionResult> ProcessAsync([FromBody] ProcessMediaRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.MediaUrl) || string.IsNullOrWhiteSpace(request.MediaType))
            return BadRequest("mediaUrl and mediaType are required.");

        var result = await mediaProcessor.ProcessAsync(request, cancellationToken);
        return Ok(result);
    }
}
