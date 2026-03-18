using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.Chat.Conversations;
using InteractiveLeads.Application.Feature.Chat.Conversations.Commands;
using InteractiveLeads.Application.Feature.Chat.Conversations.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Crm;

/// <summary>
/// Chat/CRM API: Conversations listing and inbox moves.
/// </summary>
[Authorize(Roles = "Owner,Manager,Agent")]
public sealed class ConversationsController : BaseApiController
{
    /// <summary>List conversations in an inbox (Agent can only access inboxes where is an active member).</summary>
    [HttpGet("/api/v1/inboxes/{inboxId:guid}/conversations")]
    [OpenApiOperation("List inbox conversations")]
    public async Task<IActionResult> ListByInboxAsync(Guid inboxId)
    {
        var response = await Sender.Send(new ListInboxConversationsQuery { InboxId = inboxId });
        return Ok(response);
    }

    /// <summary>Move a conversation to another inbox (Owner/Manager only).</summary>
    [HttpPut("{conversationId:guid}/move")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Move conversation to inbox (Owner/Manager only)")]
    public async Task<IActionResult> MoveAsync(Guid conversationId, [FromBody] MoveConversationRequest request)
    {
        var response = await Sender.Send(new MoveConversationToInboxCommand
        {
            ConversationId = conversationId,
            TargetInboxId = request.TargetInboxId,
            Reason = request.Reason
        });
        return Ok(response);
    }
}

