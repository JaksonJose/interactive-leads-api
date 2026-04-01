using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.Chat.Conversations;
using InteractiveLeads.Application.Feature.Chat.Conversations.Commands;
using InteractiveLeads.Application.Feature.Chat.Conversations.Queries;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Feature.Chat.Messages.Commands;
using InteractiveLeads.Application.Feature.Chat.Messages;
using InteractiveLeads.Application.Feature.Chat.Messages.Queries;
using InteractiveLeads.Application.Feature.Chat.Media;
using InteractiveLeads.Application.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Crm;

/// <summary>
/// Chat/CRM API: Conversations listing and inbox moves.
/// </summary>
[Authorize(Roles = "Owner,Manager,Agent")]
public sealed class ConversationsController(IConversationMediaUploadService conversationMediaUpload) : BaseApiController
{
    private readonly IConversationMediaUploadService _conversationMediaUpload = conversationMediaUpload;
    /// <summary>List conversations in an inbox (Agent can only access inboxes where is an active member).</summary>
    [HttpGet("/api/v1/inboxes/{inboxId:guid}/conversations")]
    [OpenApiOperation("List inbox conversations")]
    public async Task<IActionResult> ListByInboxAsync(Guid inboxId, [FromQuery] Guid? teamId = null)
    {
        var response = await Sender.Send(new ListInboxConversationsQuery { InboxId = inboxId, TeamId = teamId });
        return Ok(response);
    }

    /// <summary>
    /// List inbox conversations with cursor-based pagination, ordered by LastMessageAt DESC.
    /// </summary>
    [HttpGet("/api/conversations")]
    [OpenApiOperation("List conversations with cursor pagination")]
    public async Task<IActionResult> ListPagedAsync(
        [FromQuery] Guid? inboxId,
        [FromQuery] DateTimeOffset? cursor,
        [FromQuery] Guid? teamId = null,
        [FromQuery] int pageSize = 30)
    {
        var response = await Sender.Send(new PagedInboxConversationsQuery
        {
            InboxId = inboxId,
            Cursor = cursor,
            TeamId = teamId,
            PageSize = pageSize
        });

        return Ok(response);
    }

    /// <summary>
    /// Single list row for realtime merge when the user gains visibility (assign / transfer / invite).
    /// </summary>
    [HttpGet("{conversationId:guid}/chat-list-item")]
    [OpenApiOperation("Get one inbox list row for chat (access-filtered)")]
    public async Task<IActionResult> GetChatListItemAsync(Guid conversationId)
    {
        var response = await Sender.Send(new GetInboxConversationListItemQuery { ConversationId = conversationId });
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
    public async Task<IActionResult> ListMessagesAsync(Guid conversationId, [FromQuery] DateTimeOffset? beforeMessageDate, [FromQuery] int pageSize = 30)
    {
        var response = await Sender.Send(new ListConversationMessagesQuery
        {
            ConversationId = conversationId,
            BeforeMessageDate = beforeMessageDate,
            PageSize = pageSize
        });

        return Ok(response);
    }

    /// <summary>
    /// WhatsApp Cloud API: calculates the 24h customer care window state (based on last inbound message).
    /// When <c>requiresTemplate=true</c>, the UI must force sending a template to re-open the window.
    /// </summary>
    [HttpGet("{conversationId:guid}/whatsapp-window")]
    [OpenApiOperation("Get WhatsApp 24h window policy for a conversation")]
    public async Task<IActionResult> GetWhatsAppWindowAsync(Guid conversationId)
    {
        var response = await Sender.Send(new GetWhatsAppConversationWindowPolicyQuery
        {
            ConversationId = conversationId
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
            ClientTimestamp = request.ClientTimestamp,
            ExternalMessageId = request.ExternalMessageId,
            Type = request.Type,
            MediaUrl = request.MediaUrl,
            Caption = request.Caption,
            MimeType = request.MimeType,
            FileName = request.FileName,
            MediaOptimizedUrl = request.MediaOptimizedUrl,
            MediaOptimizedMimeType = request.MediaOptimizedMimeType,
            MediaOptimizedFileName = request.MediaOptimizedFileName,
            MediaThumbnailUrl = request.MediaThumbnailUrl,
            Voice = request.Voice,
            ReactionEmoji = request.ReactionEmoji,
            ReactionMessageId = request.ReactionMessageId,
            ReplyToMessageId = request.ReplyToMessageId,
            TemplateId = request.TemplateId,
            TemplateBodyParameters = request.TemplateBodyParameters,
            TemplateHeaderParameter = request.TemplateHeaderParameter
        });

        return Ok(response);
    }

    /// <summary>Upload a file for outbound WhatsApp media (S3). Client then POSTs /messages per item.</summary>
    [HttpPost("{conversationId:guid}/media")]
    [RequestSizeLimit(104_857_600)]
    [Consumes("multipart/form-data")]
    [OpenApiOperation("Upload conversation media file")]
    public async Task<IActionResult> UploadConversationMediaAsync(
        Guid conversationId,
        [FromForm] IFormFile file,
        [FromQuery] string? mediaType,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest();

        await using var stream = file.OpenReadStream();
        var dto = await _conversationMediaUpload.UploadAsync(
            new UploadConversationMediaRequest
            {
                ConversationId = conversationId,
                Content = stream,
                FileName = file.FileName,
                ContentType = file.ContentType ?? string.Empty,
                ContentLength = file.Length,
                MediaType = mediaType
            },
            cancellationToken);

        return Ok(new SingleResponse<ConversationMediaUploadResultDto>(dto));
    }

    /// <summary>Directory of users for assigning responsibility (inbox Agents) or inviting participants (tenant).</summary>
    [HttpGet("/api/v1/conversations/chat-directory")]
    [OpenApiOperation("Chat user directory (presence merged)")]
    public async Task<IActionResult> GetChatDirectoryAsync(
        [FromQuery] string mode = "participant",
        [FromQuery] Guid? inboxId = null,
        [FromQuery] Guid? teamId = null)
    {
        var m = string.Equals(mode, "responsible", StringComparison.OrdinalIgnoreCase)
            ? ChatDirectoryMode.Responsible
            : ChatDirectoryMode.Participant;

        var response = await Sender.Send(new GetChatDirectoryQuery { Mode = m, InboxId = inboxId, TeamId = teamId });
        return Ok(response);
    }

    /// <summary>List internal participants for a conversation.</summary>
    [HttpGet("{conversationId:guid}/participants")]
    [OpenApiOperation("List conversation participants")]
    public async Task<IActionResult> ListParticipantsAsync(Guid conversationId)
    {
        var response = await Sender.Send(new ListConversationParticipantsQuery { ConversationId = conversationId });
        return Ok(response);
    }

    /// <summary>Assign responsible agent (Owner/Manager). Target must be Agent and active inbox member.</summary>
    [HttpPut("{conversationId:guid}/assign-responsible")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Assign conversation responsible")]
    public async Task<IActionResult> AssignResponsibleAsync(Guid conversationId, [FromBody] AssignResponsibleRequest request)
    {
        if (request is null) return BadRequest();

        var response = await Sender.Send(new AssignConversationResponsibleCommand
        {
            ConversationId = conversationId,
            ResponsibleUserId = request.ResponsibleUserId
        });
        return Ok(response);
    }

    /// <summary>Transfer responsibility (Owner/Manager any conversation; Agent only if current responsible).</summary>
    [HttpPut("{conversationId:guid}/transfer-responsible")]
    [OpenApiOperation("Transfer conversation responsible")]
    public async Task<IActionResult> TransferResponsibleAsync(Guid conversationId, [FromBody] TransferResponsibleRequest request)
    {
        if (request is null) return BadRequest();

        var response = await Sender.Send(new TransferConversationResponsibleCommand
        {
            ConversationId = conversationId,
            NewResponsibleUserId = request.NewResponsibleUserId
        });
        return Ok(response);
    }

    /// <summary>Add internal collaborator (participant) without inbox membership.</summary>
    [HttpPost("{conversationId:guid}/participants")]
    [OpenApiOperation("Add conversation participant")]
    public async Task<IActionResult> AddParticipantAsync(Guid conversationId, [FromBody] AddParticipantRequestBody request)
    {
        if (request is null) return BadRequest();

        var response = await Sender.Send(new AddConversationParticipantCommand
        {
            ConversationId = conversationId,
            UserId = request.UserId
        });
        return Ok(response);
    }

    /// <summary>Remove internal participant (cannot remove current responsible).</summary>
    [HttpDelete("{conversationId:guid}/participants/{userId:guid}")]
    [OpenApiOperation("Remove conversation participant")]
    public async Task<IActionResult> RemoveParticipantAsync(Guid conversationId, Guid userId)
    {
        var response = await Sender.Send(new RemoveConversationParticipantCommand
        {
            ConversationId = conversationId,
            UserId = userId
        });
        return Ok(response);
    }
}

