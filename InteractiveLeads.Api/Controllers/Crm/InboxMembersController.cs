using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.Chat.InboxMembers.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Crm;

/// <summary>
/// Chat/CRM API: lists users with inbox access via linked teams (read-only projection).
/// </summary>
[Authorize(Roles = "Owner,Manager,Agent")]
public sealed class InboxMembersController : BaseApiController
{
    /// <summary>Users derived from active teams linked to the inbox.</summary>
    [HttpGet("/api/v1/inboxes/{inboxId:guid}/members")]
    [OpenApiOperation("List inbox access users (via teams)")]
    public async Task<IActionResult> ListAsync(Guid inboxId)
    {
        var response = await Sender.Send(new ListInboxMembersQuery { InboxId = inboxId });
        return Ok(response);
    }
}
