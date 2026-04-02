using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Feature.Chat.Conversations;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Conversations.Queries;

/// <summary>
/// Returns one inbox list row for a conversation if the current user may see it (same rules as paged list).
/// Used by the chat UI to merge a conversation in real time when the user gains access (assign / transfer / invite).
/// </summary>
public sealed class GetInboxConversationListItemQuery : IApplicationRequest<IResponse>
{
    public Guid ConversationId { get; set; }
}

public sealed class GetInboxConversationListItemQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IUserSummaryLookupService userSummaryLookup) : IApplicationRequestHandler<GetInboxConversationListItemQuery, IResponse>
{
    public async Task<IResponse> Handle(GetInboxConversationListItemQuery request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);
        var query = ChatContext.ApplyConversationAccessFilter(
                db,
                currentUserService,
                companyId,
                db.Conversations.AsNoTracking())
            .Where(c => c.Id == request.ConversationId);

        var dto = await query
            .Select(c => new InboxConversationListItemDto
            {
                Id = c.Id,
                InboxId = c.InboxId,
                ContactId = c.ContactId,
                ContactName = c.Contact.Name,
                LastMessage = c.LastMessage,
                LastMessageAt = c.LastMessageAt,
                LastMessageFromCustomer = c.LastMessageFromCustomer,
                CustomerInactivityReassignTimeoutMinutes = c.HandlingTeam != null
                    && c.HandlingTeam.AutoAssignEnabled
                    && c.HandlingTeam.AutoAssignReassignTimeoutMinutes != null
                    && c.HandlingTeam.AutoAssignReassignTimeoutMinutes.Value > 0
                    ? c.HandlingTeam.AutoAssignReassignTimeoutMinutes
                    : null,
                CreatedAt = c.CreatedAt,
                InboxName = c.Inbox.Name,
                Status = c.Status,
                AssignedAgentId = c.AssignedAgentId,
                EffectiveSlaPolicyId = c.EffectiveSlaPolicyId,
                FirstResponseDueAt = c.FirstResponseDueAt,
                ResolutionDueAt = c.ResolutionDueAt,
                FirstAgentResponseAt = c.FirstAgentResponseAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            var notFound = new ResultResponse();
            notFound.AddErrorMessage("Conversation not found.", "general.not_found");
            throw new NotFoundException(notFound);
        }

        dto.ApplySlaBreachFlags(DateTimeOffset.UtcNow);

        if (dto.AssignedAgentId.HasValue)
        {
            var key = dto.AssignedAgentId.Value.ToString();
            var summaries = await userSummaryLookup.GetSummariesByIdsAsync(new[] { key }, cancellationToken);
            if (summaries.TryGetValue(key, out var summary))
                dto.AssignedAgentName = summary.DisplayName;
        }

        return new SingleResponse<InboxConversationListItemDto>(dto);
    }
}
