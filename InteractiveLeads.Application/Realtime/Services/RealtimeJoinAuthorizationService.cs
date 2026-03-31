using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Realtime.Services;

public class RealtimeJoinAuthorizationService : IRealtimeJoinAuthorizationService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public RealtimeJoinAuthorizationService(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task EnsureCanJoinInboxAsync(string inboxId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(inboxId, out var inboxGuid))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Invalid inbox id.", "general.bad_request");
            throw new BadRequestException(response);
        }

        var companyId = await ChatContext.GetCompanyIdAsync(_db, _currentUserService, cancellationToken);
        await ChatContext.EnsureInboxAccessAsync(_db, _currentUserService, inboxGuid, companyId, cancellationToken);
    }

    public async Task EnsureCanJoinConversationAsync(string conversationId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(conversationId, out var conversationGuid))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Invalid conversation id.", "general.bad_request");
            throw new BadRequestException(response);
        }

        var companyId = await ChatContext.GetCompanyIdAsync(_db, _currentUserService, cancellationToken);

        var conversationInboxId = await _db.Conversations
            .AsNoTracking()
            .Where(c => c.Id == conversationGuid && c.CompanyId == companyId)
            .Select(c => c.InboxId)
            .SingleOrDefaultAsync(cancellationToken);

        if (conversationInboxId == Guid.Empty)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Conversation not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        await ChatContext.EnsureConversationCollaborationAccessAsync(
            _db,
            _currentUserService,
            conversationGuid,
            conversationInboxId,
            companyId,
            cancellationToken);
    }
}

