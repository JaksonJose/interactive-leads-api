using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Application.Dispatching;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.InboxMembers.Commands;

public sealed class AddInboxMemberCommand : IApplicationRequest<IResponse>
{
    public Guid InboxId { get; set; }
    public AddInboxMemberRequest AddInboxMember { get; set; } = new();
}

public sealed class AddInboxMemberCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<AddInboxMemberCommand, IResponse>
{
    public async Task<IResponse> Handle(AddInboxMemberCommand request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var inboxExists = await db.Inboxes
            .AsNoTracking()
            .AnyAsync(i => i.Id == request.InboxId && i.CompanyId == companyId, cancellationToken);
        if (!inboxExists)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Inbox not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        var userId = (request.AddInboxMember?.UserId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(userId))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("UserId is required.", "chat.inbox_member.user_required");
            throw new BadRequestException(response);
        }

        var existing = await db.InboxMembers
            .Where(m => m.InboxId == request.InboxId && m.UserId == userId)
            .OrderByDescending(m => m.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing != null)
        {
            existing.IsActive = request.AddInboxMember?.IsActive ?? true;
            existing.Role = request.AddInboxMember?.Role;
            existing.CanBeAssigned = request.AddInboxMember?.CanBeAssigned ?? true;
            if (existing.JoinedAt == default)
                existing.JoinedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(cancellationToken);

            return new SingleResponse<InboxMemberDto>(new InboxMemberDto
            {
                Id = existing.Id,
                InboxId = existing.InboxId,
                UserId = existing.UserId,
                Role = existing.Role,
                IsActive = existing.IsActive,
                CanBeAssigned = existing.CanBeAssigned,
                JoinedAt = existing.JoinedAt
            });
        }

        var member = new InboxMember
        {
            Id = Guid.NewGuid(),
            InboxId = request.InboxId,
            UserId = userId,
            Role = request.AddInboxMember?.Role,
            IsActive = request.AddInboxMember?.IsActive ?? true,
            CanBeAssigned = request.AddInboxMember?.CanBeAssigned ?? true,
            JoinedAt = DateTimeOffset.UtcNow
        };

        db.InboxMembers.Add(member);
        await db.SaveChangesAsync(cancellationToken);

        return new SingleResponse<InboxMemberDto>(new InboxMemberDto
        {
            Id = member.Id,
            InboxId = member.InboxId,
            UserId = member.UserId,
            Role = member.Role,
            IsActive = member.IsActive,
            CanBeAssigned = member.CanBeAssigned,
            JoinedAt = member.JoinedAt
        });
    }
}


