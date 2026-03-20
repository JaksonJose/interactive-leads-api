using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.Chat.Conversations;
using InteractiveLeads.Application.Feature.Chat.Conversations.Commands;
using InteractiveLeads.Application.Feature.Chat.Conversations.Queries;
using InteractiveLeads.Application.Feature.Chat.Messages.Commands;
using InteractiveLeads.Application.Feature.Chat.Messages;
using InteractiveLeads.Application.Feature.Chat.Messages.Queries;
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

    /// <summary>
    /// List inbox conversations with cursor-based pagination, ordered by LastMessageAt DESC.
    /// </summary>
    [HttpGet("/api/conversations")]
    [OpenApiOperation("List conversations with cursor pagination")]
    public async Task<IActionResult> ListPagedAsync([FromQuery] Guid? inboxId, [FromQuery] DateTimeOffset? cursor, [FromQuery] int pageSize = 30)
    {
        var response = await Sender.Send(new PagedInboxConversationsQuery
        {
            InboxId = inboxId,
            Cursor = cursor,
            PageSize = pageSize
        });

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

    /// <summary>
    /// List messages of a conversation with cursor-based pagination (most recent at the end).
    /// </summary>
    [HttpGet("{conversationId:guid}/messages")]
    [OpenApiOperation("List conversation messages with cursor pagination")]
    public async Task<IActionResult> ListMessagesAsync(Guid conversationId, [FromQuery] DateTimeOffset? beforeCreatedAt, [FromQuery] int pageSize = 30)
    {
        var response = await Sender.Send(new ListConversationMessagesQuery
        {
            ConversationId = conversationId,
            BeforeCreatedAt = beforeCreatedAt,
            PageSize = pageSize
        });

        return Ok(response);
    }

    /// <summary>
    /// Send a message from chat to the backend (MVP: persist outbound only).
    /// </summary>
    [HttpPost("{conversationId:guid}/messages")]
    [OpenApiOperation("Send a message (persist outbound message)")]
    public async Task<IActionResult> SendMessageAsync(Guid conversationId, [FromBody] SendConversationMessageRequest request)
    {
        if (request == null)
            return BadRequest();

        var response = await Sender.Send(new SendConversationMessageCommand
        {
            ConversationId = conversationId,
            Content = request.Content,
            ExternalMessageId = request.ExternalMessageId,
            Type = request.Type,
            MediaUrl = request.MediaUrl,
            Caption = request.Caption,
            ReactionEmoji = request.ReactionEmoji,
            ReactionMessageId = request.ReactionMessageId,
            ReplyToMessageId = request.ReplyToMessageId
        });

        return Ok(response);
    }
}

