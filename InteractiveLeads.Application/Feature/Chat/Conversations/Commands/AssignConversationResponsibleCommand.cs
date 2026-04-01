using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Conversations.Commands;

public sealed class AssignConversationResponsibleCommand : IApplicationRequest<IResponse>
{
    public Guid ConversationId { get; set; }
    public Guid ResponsibleUserId { get; set; }
}

public sealed class AssignConversationResponsibleCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IChatConversationUserValidator userValidator,
    IConversationCollaborationRealtimePublisher collaborationRealtime) : IApplicationRequestHandler<AssignConversationResponsibleCommand, IResponse>
{
    public async Task<IResponse> Handle(AssignConversationResponsibleCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsInRole("Owner") && !currentUserService.IsInRole("Manager"))
        {
            var forbidden = new ResultResponse();
            forbidden.AddErrorMessage("Only Owner or Manager can assign a responsible.", "general.access_denied");
            throw new ForbiddenException(forbidden);
        }

        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var conversation = await db.Conversations
            .Where(c => c.Id == request.ConversationId && c.CompanyId == companyId)
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

        await userValidator.EnsureValidResponsibleTargetAsync(request.ResponsibleUserId, conversation.InboxId, cancellationToken);

        conversation.AssignedAgentId = request.ResponsibleUserId;

        await EnsureInternalParticipantRowAsync(db, conversation.Id, request.ResponsibleUserId, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await collaborationRealtime.PublishCollaborationUpdatedAsync(conversation, cancellationToken);
        return new ResultResponse();
    }

    private static async Task EnsureInternalParticipantRowAsync(
        IApplicationDbContext db,
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var uid = userId.ToString("D");
        var existing = await db.ConversationParticipants
            .Where(p => p.ConversationId == conversationId && p.UserId == uid && p.Role == ConversationParticipantRole.Agent)
            .SingleOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            existing.IsActive = true;
            existing.LeftAt = null;
            return;
        }

        db.ConversationParticipants.Add(new ConversationParticipant
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            UserId = uid,
            ContactId = null,
            Role = ConversationParticipantRole.Agent,
            JoinedAt = DateTimeOffset.UtcNow,
            LeftAt = null,
            IsActive = true
        });
    }
}
