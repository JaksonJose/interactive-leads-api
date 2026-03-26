using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Application.Dispatching;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.InboxMembers.Commands;

public sealed class UpdateInboxMemberCommand : IApplicationRequest<IResponse>
{
    public Guid InboxId { get; set; }
    public Guid MemberId { get; set; }
    public UpdateInboxMemberRequest UpdateInboxMember { get; set; } = new();
}

public sealed class UpdateInboxMemberCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<UpdateInboxMemberCommand, IResponse>
{
    public async Task<IResponse> Handle(UpdateInboxMemberCommand request, CancellationToken cancellationToken)
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

        var member = await db.InboxMembers
            .Where(m => m.Id == request.MemberId && m.InboxId == request.InboxId)
            .SingleOrDefaultAsync(cancellationToken);

        if (member == null)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Inbox member not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        member.Role = request.UpdateInboxMember?.Role;
        member.IsActive = request.UpdateInboxMember?.IsActive ?? member.IsActive;
        if (request.UpdateInboxMember?.CanBeAssigned != null)
            member.CanBeAssigned = request.UpdateInboxMember.CanBeAssigned.Value;

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


