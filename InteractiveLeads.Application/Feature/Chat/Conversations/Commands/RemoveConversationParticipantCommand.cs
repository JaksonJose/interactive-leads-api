using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Conversations.Commands;

public sealed class RemoveConversationParticipantCommand : IApplicationRequest<IResponse>
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
}

public sealed class RemoveConversationParticipantCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IConversationCollaborationRealtimePublisher collaborationRealtime) : IApplicationRequestHandler<RemoveConversationParticipantCommand, IResponse>
{
    public async Task<IResponse> Handle(RemoveConversationParticipantCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsInRole("Owner") && !currentUserService.IsInRole("Manager") && !currentUserService.IsInRole("Agent"))
        {
            var forbidden = new ResultResponse();
            forbidden.AddErrorMessage("You are not allowed to remove participants.", "general.access_denied");
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

        var uid = request.UserId.ToString("D");

        if (conversation.AssignedAgentId == request.UserId)
        {
            var conflict = new ResultResponse();
            conflict.AddErrorMessage("Cannot remove the current responsible; transfer responsibility first.", "chat.cannot_remove_responsible");
            throw new ConflictException(conflict);
        }

        var row = await db.ConversationParticipants
            .Where(p =>
                p.ConversationId == request.ConversationId &&
                p.UserId == uid &&
                p.Role == ConversationParticipantRole.Agent)
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null || !row.IsActive)
            return new ResultResponse();

        row.IsActive = false;
        row.LeftAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await collaborationRealtime.PublishCollaborationUpdatedAsync(conversation, cancellationToken);
        return new ResultResponse();
    }
}
