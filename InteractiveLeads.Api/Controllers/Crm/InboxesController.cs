using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.Chat.Inboxes;
using InteractiveLeads.Application.Feature.Chat.Inboxes.Commands;
using InteractiveLeads.Application.Feature.Chat.Inboxes.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Crm;

/// <summary>
/// Chat/CRM API: Inbox CRUD and activation.
/// </summary>
[Authorize(Roles = "Owner,Manager,Agent")]
public sealed class InboxesController : BaseApiController
{
    /// <summary>List inboxes available to the current user (Agent sees only inboxes where is an active member).</summary>
    [HttpGet]
    [OpenApiOperation("List inboxes")]
    public async Task<IActionResult> ListAsync([FromQuery] bool includeInactive = true)
    {
        var response = await Sender.Send(new ListInboxesQuery { IncludeInactive = includeInactive });
        return Ok(response);
    }

    /// <summary>
    /// List inboxes available to the current user for chat filters, returning only id and name.
    /// </summary>
    [HttpGet("/api/inboxes")]
    [OpenApiOperation("List available inboxes for chat filters")]
    public async Task<IActionResult> ListForChatAsync()
    {
        var response = await Sender.Send(new ListInboxesQuery { IncludeInactive = false });
        return Ok(response);
    }

    /// <summary>Get an inbox by id (Agent must be an active member).</summary>
    [HttpGet("{inboxId:guid}")]
    [OpenApiOperation("Get inbox by id")]
    public async Task<IActionResult> GetByIdAsync(Guid inboxId)
    {
        var response = await Sender.Send(new GetInboxQuery { InboxId = inboxId });
        return Ok(response);
    }

    /// <summary>Create a new inbox (Owner/Manager only).</summary>
    [HttpPost]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Create inbox (Owner/Manager only)")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateInboxRequest request)
    {
        var response = await Sender.Send(new CreateInboxCommand { CreateInbox = request });
        return Ok(response);
    }

    /// <summary>Update an inbox (Owner/Manager only).</summary>
    [HttpPut("{inboxId:guid}")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Update inbox (Owner/Manager only)")]
    public async Task<IActionResult> UpdateAsync(Guid inboxId, [FromBody] UpdateInboxRequest request)
    {
        var response = await Sender.Send(new UpdateInboxCommand { InboxId = inboxId, UpdateInbox = request });
        return Ok(response);
    }

    /// <summary>Activate an inbox (Owner/Manager only).</summary>
    [HttpPut("{inboxId:guid}/activate")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Activate inbox (Owner/Manager only)")]
    public async Task<IActionResult> ActivateAsync(Guid inboxId)
    {
        var response = await Sender.Send(new SetInboxActiveCommand { InboxId = inboxId, IsActive = true });
        return Ok(response);
    }

    /// <summary>Deactivate an inbox (Owner/Manager only).</summary>
    [HttpPut("{inboxId:guid}/deactivate")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Deactivate inbox (Owner/Manager only)")]
    public async Task<IActionResult> DeactivateAsync(Guid inboxId)
    {
        var response = await Sender.Send(new SetInboxActiveCommand { InboxId = inboxId, IsActive = false });
        return Ok(response);
    }
}

