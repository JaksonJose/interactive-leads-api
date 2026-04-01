using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Conversations.Commands;

public sealed class AddConversationParticipantCommand : IApplicationRequest<IResponse>
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
}

public sealed class AddConversationParticipantCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IChatConversationUserValidator userValidator,
    IConversationCollaborationRealtimePublisher collaborationRealtime) : IApplicationRequestHandler<AddConversationParticipantCommand, IResponse>
{
    public async Task<IResponse> Handle(AddConversationParticipantCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsInRole("Owner") && !currentUserService.IsInRole("Manager") && !currentUserService.IsInRole("Agent"))
        {
            var forbidden = new ResultResponse();
            forbidden.AddErrorMessage("You are not allowed to add participants.", "general.access_denied");
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

        await userValidator.EnsureValidParticipantTargetAsync(request.UserId, cancellationToken);

        var uid = request.UserId.ToString("D");

        var existing = await db.ConversationParticipants
            .Where(p =>
                p.ConversationId == request.ConversationId &&
                p.UserId == uid &&
                p.Role == ConversationParticipantRole.Agent)
            .SingleOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            if (existing.IsActive)
                return new ResultResponse();

            existing.IsActive = true;
            existing.LeftAt = null;
            existing.JoinedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            db.ConversationParticipants.Add(new ConversationParticipant
            {
                Id = Guid.NewGuid(),
                ConversationId = request.ConversationId,
                UserId = uid,
                ContactId = null,
                Role = ConversationParticipantRole.Agent,
                JoinedAt = DateTimeOffset.UtcNow,
                LeftAt = null,
                IsActive = true
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await collaborationRealtime.PublishCollaborationUpdatedAsync(conversation, cancellationToken);
        return new ResultResponse();
    }
}
