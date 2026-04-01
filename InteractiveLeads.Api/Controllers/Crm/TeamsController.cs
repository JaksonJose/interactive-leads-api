using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.Crm.Teams;
using InteractiveLeads.Application.Feature.Crm.Teams.Commands;
using InteractiveLeads.Application.Feature.Crm.Teams.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Crm;

/// <summary>CRM Teams: logical user groupings per company.</summary>
[Authorize(Roles = "Owner,Manager,Agent")]
public sealed class TeamsController : BaseApiController
{
    [HttpGet]
    [OpenApiOperation("List teams for the current company")]
    public async Task<IActionResult> ListByCompanyAsync()
    {
        var response = await Sender.Send(new GetTeamsByCompanyQuery());
        return Ok(response);
    }

    [HttpGet("by-user")]
    [OpenApiOperation("List teams for a user")]
    public async Task<IActionResult> ListByUserAsync([FromQuery] string? userId = null)
    {
        var response = await Sender.Send(new GetTeamsByUserQuery { UserId = userId });
        return Ok(response);
    }

    [HttpGet("by-inbox/{inboxId:guid}")]
    [OpenApiOperation("List teams linked to an inbox (legacy path; prefer GET /Inboxes/{inboxId}/teams)")]
    public async Task<IActionResult> ListByInboxAsync(Guid inboxId)
    {
        var response = await Sender.Send(new GetTeamsByInboxQuery { InboxId = inboxId });
        return Ok(response);
    }

    [HttpGet("{teamId:guid}")]
    [OpenApiOperation("Get team by id")]
    public async Task<IActionResult> GetByIdAsync(Guid teamId)
    {
        var response = await Sender.Send(new GetTeamByIdQuery { TeamId = teamId });
        return Ok(response);
    }

    [HttpGet("{teamId:guid}/inboxes")]
    [OpenApiOperation("List inboxes linked to a team")]
    public async Task<IActionResult> ListInboxesForTeamAsync(Guid teamId)
    {
        var response = await Sender.Send(new GetInboxesByTeamQuery { TeamId = teamId });
        return Ok(response);
    }

    [HttpGet("{teamId:guid}/members")]
    [OpenApiOperation("List members of a team")]
    public async Task<IActionResult> ListMembersAsync(Guid teamId)
    {
        var response = await Sender.Send(new GetTeamMembersQuery { TeamId = teamId });
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Create team")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateTeamRequest request)
    {
        var response = await Sender.Send(new CreateTeamCommand { CreateTeam = request });
        return Ok(response);
    }

    [HttpPut("{teamId:guid}")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Update team")]
    public async Task<IActionResult> UpdateAsync(Guid teamId, [FromBody] UpdateTeamRequest request)
    {
        var response = await Sender.Send(new UpdateTeamCommand { TeamId = teamId, UpdateTeam = request });
        return Ok(response);
    }

    [HttpDelete("{teamId:guid}")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Deactivate team (soft delete)")]
    public async Task<IActionResult> DeleteAsync(Guid teamId)
    {
        var response = await Sender.Send(new DeleteTeamCommand { TeamId = teamId });
        return Ok(response);
    }

    [HttpPost("{teamId:guid}/members")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Add user to team")]
    public async Task<IActionResult> AddMemberAsync(Guid teamId, [FromBody] AddUserToTeamRequest request)
    {
        var response = await Sender.Send(new AddUserToTeamCommand { TeamId = teamId, AddUser = request });
        return Ok(response);
    }

    [HttpDelete("{teamId:guid}/members/{userId}")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Remove user from team")]
    public async Task<IActionResult> RemoveMemberAsync(Guid teamId, string userId)
    {
        var response = await Sender.Send(new RemoveUserFromTeamCommand { TeamId = teamId, UserId = userId });
        return Ok(response);
    }

    [HttpPost("{teamId:guid}/inboxes/{inboxId:guid}")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Link team to inbox")]
    public async Task<IActionResult> LinkInboxAsync(Guid teamId, Guid inboxId)
    {
        var response = await Sender.Send(new LinkTeamToInboxCommand { TeamId = teamId, InboxId = inboxId });
        return Ok(response);
    }

    [HttpDelete("{teamId:guid}/inboxes/{inboxId:guid}")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Unlink team from inbox")]
    public async Task<IActionResult> UnlinkInboxAsync(Guid teamId, Guid inboxId)
    {
        var response = await Sender.Send(new UnlinkTeamFromInboxCommand { TeamId = teamId, InboxId = inboxId });
        return Ok(response);
    }
}
