using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.Crm.Integrations;
using InteractiveLeads.Application.Feature.Crm.Integrations.Commands;
using InteractiveLeads.Application.Feature.Crm.Integrations.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Crm;

/// <summary>
/// CRM API: Integrations CRUD for company-level providers.
/// </summary>
[Authorize(Roles = "Owner,Manager")]
public sealed class IntegrationsController : BaseApiController
{
    /// <summary>List integrations for the current company.</summary>
    [HttpGet]
    [OpenApiOperation("List integrations")]
    public async Task<IActionResult> ListAsync([FromQuery] bool includeInactive = true)
    {
        var response = await Sender.Send(new ListIntegrationsQuery { IncludeInactive = includeInactive });
        return Ok(response);
    }

    /// <summary>Get an integration by id for the current company.</summary>
    [HttpGet("{integrationId:guid}")]
    [OpenApiOperation("Get integration by id")]
    public async Task<IActionResult> GetByIdAsync(Guid integrationId)
    {
        var response = await Sender.Send(new GetIntegrationQuery { IntegrationId = integrationId });
        return Ok(response);
    }

    /// <summary>Create a new integration (Owner/Manager only).</summary>
    [HttpPost]
    [OpenApiOperation("Create integration (Owner/Manager only)")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateIntegrationRequest request)
    {
        var response = await Sender.Send(new CreateIntegrationCommand { Integration = request });
        return Ok(response);
    }

    /// <summary>Update an integration (Owner/Manager only).</summary>
    [HttpPut("{integrationId:guid}")]
    [OpenApiOperation("Update integration (Owner/Manager only)")]
    public async Task<IActionResult> UpdateAsync(Guid integrationId, [FromBody] UpdateIntegrationRequest request)
    {
        var response = await Sender.Send(new UpdateIntegrationCommand { IntegrationId = integrationId, Integration = request });
        return Ok(response);
    }

    /// <summary>Delete an integration (Owner/Manager only).</summary>
    [HttpDelete("{integrationId:guid}")]
    [OpenApiOperation("Delete integration (Owner/Manager only)")]
    public async Task<IActionResult> DeleteAsync(Guid integrationId)
    {
        await Sender.Send(new DeleteIntegrationCommand { IntegrationId = integrationId });
        return Ok(new { });
    }
}

