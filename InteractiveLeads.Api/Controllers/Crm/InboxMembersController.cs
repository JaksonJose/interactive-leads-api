using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.Chat.InboxMembers;
using InteractiveLeads.Application.Feature.Chat.InboxMembers.Commands;
using InteractiveLeads.Application.Feature.Chat.InboxMembers.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Crm;

/// <summary>
/// Chat/CRM API: Inbox members management.
/// </summary>
[Authorize(Roles = "Owner,Manager,Agent")]
public sealed class InboxMembersController : BaseApiController
{
    /// <summary>List members in an inbox. Agent can only access inboxes where is an active member.</summary>
    [HttpGet("/api/v1/inboxes/{inboxId:guid}/members")]
    [OpenApiOperation("List inbox members")]
    public async Task<IActionResult> ListAsync(Guid inboxId)
    {
        var response = await Sender.Send(new ListInboxMembersQuery { InboxId = inboxId });
        return Ok(response);
    }

    /// <summary>Add a member to an inbox (Owner/Manager only).</summary>
    [HttpPost("/api/v1/inboxes/{inboxId:guid}/members")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Add inbox member (Owner/Manager only)")]
    public async Task<IActionResult> AddAsync(Guid inboxId, [FromBody] AddInboxMemberRequest request)
    {
        var response = await Sender.Send(new AddInboxMemberCommand { InboxId = inboxId, AddInboxMember = request });
        return Ok(response);
    }

    /// <summary>Update a member in an inbox (Owner/Manager only).</summary>
    [HttpPut("/api/v1/inboxes/{inboxId:guid}/members/{memberId:guid}")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Update inbox member (Owner/Manager only)")]
    public async Task<IActionResult> UpdateAsync(Guid inboxId, Guid memberId, [FromBody] UpdateInboxMemberRequest request)
    {
        var response = await Sender.Send(new UpdateInboxMemberCommand
        {
            InboxId = inboxId,
            MemberId = memberId,
            UpdateInboxMember = request
        });
        return Ok(response);
    }
}

