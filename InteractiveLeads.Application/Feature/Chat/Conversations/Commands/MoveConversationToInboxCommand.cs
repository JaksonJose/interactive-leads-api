using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Application.Dispatching;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Conversations.Commands;

public sealed class MoveConversationToInboxCommand : IApplicationRequest<IResponse>
{
    public Guid ConversationId { get; set; }
    public Guid TargetInboxId { get; set; }
    public string? Reason { get; set; }
}

public sealed class MoveConversationToInboxCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<MoveConversationToInboxCommand, IResponse>
{
    public async Task<IResponse> Handle(MoveConversationToInboxCommand request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var conversation = await db.Conversations
            .Where(c => c.Id == request.ConversationId && c.CompanyId == companyId)
            .SingleOrDefaultAsync(cancellationToken);

        if (conversation == null)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Conversation not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        var targetInboxExists = await db.Inboxes
            .AsNoTracking()
            .AnyAsync(i => i.Id == request.TargetInboxId && i.CompanyId == companyId, cancellationToken);

        if (!targetInboxExists)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Target inbox not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        if (conversation.InboxId == request.TargetInboxId)
            return new ResultResponse();

        var movedBy = currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(movedBy))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Invalid user ID", "general.unauthorized");
            throw new UnauthorizedException(response);
        }

        var movement = new ConversationInboxMovement
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            FromInboxId = conversation.InboxId,
            ToInboxId = request.TargetInboxId,
            MovedByUserId = movedBy,
            MovedAt = DateTimeOffset.UtcNow,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim()
        };

        conversation.InboxId = request.TargetInboxId;
        db.ConversationInboxMovements.Add(movement);

        await db.SaveChangesAsync(cancellationToken);

        return new ResultResponse();
    }
}


