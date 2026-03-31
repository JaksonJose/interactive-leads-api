using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Conversations.Queries;

public sealed class ListConversationParticipantsQuery : IApplicationRequest<IResponse>
{
    public Guid ConversationId { get; set; }
}

public sealed class ListConversationParticipantsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IUserSummaryLookupService userSummaryLookup) : IApplicationRequestHandler<ListConversationParticipantsQuery, IResponse>
{
    public async Task<IResponse> Handle(ListConversationParticipantsQuery request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var conversation = await db.Conversations
            .AsNoTracking()
            .Where(c => c.Id == request.ConversationId && c.CompanyId == companyId)
            .Select(c => new { c.Id, c.InboxId })
            .SingleOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            var notFound = new ResultResponse();
            notFound.AddErrorMessage("Conversation not found.", "general.not_found");
            throw new NotFoundException(notFound);
        }

        await ChatContext.EnsureConversationCollaborationAccessAsync(
            db,
            currentUserService,
            conversation.Id,
            conversation.InboxId,
            companyId,
            cancellationToken);

        var rows = await db.ConversationParticipants
            .AsNoTracking()
            .Where(p =>
                p.ConversationId == request.ConversationId &&
                p.Role == ConversationParticipantRole.Agent &&
                p.UserId != null &&
                p.UserId != "")
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.JoinedAt)
            .Select(p => new
            {
                UserId = p.UserId!,
                p.JoinedAt,
                p.IsActive
            })
            .ToListAsync(cancellationToken);

        var ids = rows.Select(r => r.UserId).Distinct().ToList();
        var summaries = await userSummaryLookup.GetSummariesByIdsAsync(ids, cancellationToken);

        var items = rows.Select(r =>
        {
            summaries.TryGetValue(r.UserId, out var s);
            var display = s.DisplayName;
            return new ConversationParticipantListItemDto
            {
                UserId = r.UserId,
                DisplayName = display,
                Email = s.Email,
                JoinedAt = r.JoinedAt,
                IsActive = r.IsActive
            };
        }).ToList();

        return new ListResponse<ConversationParticipantListItemDto>(items, items.Count);
    }
}
