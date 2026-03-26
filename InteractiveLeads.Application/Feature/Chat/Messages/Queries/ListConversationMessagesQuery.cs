using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Enums;
using InteractiveLeads.Application.Dispatching;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InteractiveLeads.Application.Feature.Chat.Messages.Queries;

public sealed class ListConversationMessagesQuery : IApplicationRequest<IResponse>
{
    public Guid ConversationId { get; set; }
    public DateTimeOffset? BeforeMessageDate { get; set; }
    public int PageSize { get; set; } = 30;
}

public sealed class ListConversationMessagesQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<ListConversationMessagesQuery, IResponse>
{
    public async Task<IResponse> Handle(ListConversationMessagesQuery request, CancellationToken cancellationToken)
    {
        var pageSize = request.PageSize <= 0 ? 30 : Math.Min(request.PageSize, 100);

        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var conversation = await db.Conversations
            .AsNoTracking()
            .Include(c => c.Inbox)
            .Where(c => c.Id == request.ConversationId && c.CompanyId == companyId)
            .SingleOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            var errorResponse = new ResultResponse();
            errorResponse.AddErrorMessage("Conversation not found.", "general.not_found");
            throw new NotFoundException(errorResponse);
        }

        await ChatContext.EnsureInboxAccessAsync(db, currentUserService, conversation.InboxId, companyId, cancellationToken);

        var messagesQuery = db.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == request.ConversationId);

        if (request.BeforeMessageDate.HasValue)
        {
            messagesQuery = messagesQuery.Where(m => m.MessageDate < request.BeforeMessageDate.Value);
        }

        var rawItems = await messagesQuery
            .OrderByDescending(m => m.MessageDate)
            .Take(pageSize + 1)
            .Select(m => new
            {
                m.Id,
                m.Content,
                Type = m.Type.ToString().ToLowerInvariant(),
                Media = m.Media
                    .OrderBy(x => x.Id)
                    .Select(x => new MessageMediaListItemDto
                    {
                        Url = x.Url,
                        MimeType = x.MimeType,
                        FileName = x.FileName,
                        Animated = x.Animated,
                        Voice = x.Voice,
                        Caption = x.Caption
                    })
                    .FirstOrDefault(),
                Direction = m.Direction == MessageDirection.Inbound ? "inbound" : "outbound",
                m.MessageDate,
                m.CreatedAt,
                m.UpdatedAt,
                Status = MessageListItemDtoMapper.ToStatusString(m.Status),
                m.Metadata
            })
            .ToListAsync(cancellationToken);

        var items = rawItems
            .Select(m =>
            {
                var processingStatus = ReadMediaProcessingStatus(m.Metadata);
                var media = m.Media;
                if (processingStatus == "processing" && media is not null)
                    media.Url = string.Empty;
                else if (media is not null && m.Direction == "outbound")
                    MergeOutboundImageMetadata(media, m.Metadata, m.Type);

                return new MessageListItemDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    Type = m.Type,
                    Media = media,
                    Direction = m.Direction,
                    MessageDate = m.MessageDate,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt,
                    Status = m.Status,
                    MediaProcessingStatus = processingStatus
                };
            })
            .ToList();

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items = items.Take(pageSize).ToList();
        }

        // Order chronologically (oldest first) for chat UI
        items = items
            .OrderBy(m => m.MessageDate)
            .ToList();

        var nextCursor = hasMore
            ? items.Min(m => m.MessageDate)
            : (DateTimeOffset?)null;

        var response = new CursorListResponse<MessageListItemDto>(items, hasMore, nextCursor);
        return response;
    }

    private static void MergeOutboundImageMetadata(MessageMediaListItemDto media, string? metadata, string messageType)
    {
        if (!string.Equals(messageType, "image", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(metadata))
            return;

        if (string.IsNullOrWhiteSpace(media.OptimizedUrl) && !string.IsNullOrWhiteSpace(media.Url))
            media.OptimizedUrl = media.Url;

        try
        {
            using var doc = JsonDocument.Parse(metadata);
            if (!doc.RootElement.TryGetProperty("outbound", out var outbound))
                return;
            if (outbound.TryGetProperty("mediaOptimizedUrl", out var opt) && opt.ValueKind == JsonValueKind.String)
            {
                var o = opt.GetString();
                if (!string.IsNullOrWhiteSpace(o))
                    media.OptimizedUrl = o;
            }
            if (outbound.TryGetProperty("mediaThumbnailUrl", out var thumb) && thumb.ValueKind == JsonValueKind.String)
            {
                var t = thumb.GetString();
                if (!string.IsNullOrWhiteSpace(t))
                    media.ThumbnailUrl = t;
            }
        }
        catch
        {
            // ignore malformed metadata
        }
    }

    private static string? ReadMediaProcessingStatus(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(metadata);
            if (!doc.RootElement.TryGetProperty("mediaProcessingStatus", out var status))
                return null;
            var value = status.GetString()?.Trim().ToLowerInvariant();
            return value is "processing" or "completed" or "failed" ? value : null;
        }
        catch
        {
            return null;
        }
    }
}


