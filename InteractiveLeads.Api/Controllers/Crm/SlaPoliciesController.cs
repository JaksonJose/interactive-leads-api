using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.Crm.SlaPolicies;
using InteractiveLeads.Application.Feature.Crm.SlaPolicies.Commands;
using InteractiveLeads.Application.Feature.Crm.SlaPolicies.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Crm;

/// <summary>SLA policies per company (targets for first response and resolution).</summary>
[Authorize(Roles = "Owner,Manager,Agent")]
public sealed class SlaPoliciesController : BaseApiController
{
    [HttpGet]
    [OpenApiOperation("List SLA policies for the current company")]
    public async Task<IActionResult> ListAsync(
        [FromQuery] bool? activeOnly = null,
        [FromQuery] DateTimeOffset? updatedAfter = null)
    {
        var response = await Sender.Send(new ListSlaPoliciesQuery
        {
            ActiveOnly = activeOnly,
            UpdatedAfter = updatedAfter
        });
        return Ok(response);
    }

    [HttpGet("{policyId:guid}")]
    [OpenApiOperation("Get SLA policy by id")]
    public async Task<IActionResult> GetByIdAsync(Guid policyId)
    {
        var response = await Sender.Send(new GetSlaPolicyByIdQuery { PolicyId = policyId });
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Create SLA policy")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateSlaPolicyRequest request)
    {
        var response = await Sender.Send(new CreateSlaPolicyCommand { Body = request });
        return Ok(response);
    }

    [HttpPut("{policyId:guid}")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Update SLA policy")]
    public async Task<IActionResult> UpdateAsync(Guid policyId, [FromBody] UpdateSlaPolicyRequest request)
    {
        var response = await Sender.Send(new UpdateSlaPolicyCommand { PolicyId = policyId, Body = request });
        return Ok(response);
    }

    [HttpDelete("{policyId:guid}")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Deactivate SLA policy (soft)")]
    public async Task<IActionResult> DeactivateAsync(Guid policyId)
    {
        var response = await Sender.Send(new DeactivateSlaPolicyCommand { PolicyId = policyId });
        return Ok(response);
    }
}
