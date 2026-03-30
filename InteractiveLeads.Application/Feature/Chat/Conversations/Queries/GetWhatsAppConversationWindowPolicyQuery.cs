using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Conversations.Queries;

public sealed class GetWhatsAppConversationWindowPolicyQuery : IApplicationRequest<IResponse>
{
    public Guid ConversationId { get; set; }
}

public sealed class WhatsAppConversationWindowPolicyDto
{
    public Guid ConversationId { get; set; }
    public string IntegrationType { get; set; } = string.Empty;
    public Guid? WhatsAppBusinessAccountId { get; set; }

    /// <summary>Last inbound message time (customer message). Null when no inbound exists.</summary>
    public DateTimeOffset? LastInboundAt { get; set; }

    /// <summary>UTC time when free-reply window ends (LastInboundAt + 24h). Null when no inbound exists.</summary>
    public DateTimeOffset? FreeReplyUntil { get; set; }

    /// <summary>When true, WhatsApp requires a template to re-open the 24h window.</summary>
    public bool RequiresTemplate { get; set; }
}

public sealed class GetWhatsAppConversationWindowPolicyQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService)
    : IApplicationRequestHandler<GetWhatsAppConversationWindowPolicyQuery, IResponse>
{
    public async Task<IResponse> Handle(GetWhatsAppConversationWindowPolicyQuery request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var conversation = await ChatContext.ApplyConversationAccessFilter(
                db,
                currentUserService,
                companyId,
                db.Conversations
                    .AsNoTracking()
                    .Include(c => c.Integration))
            .Where(c => c.Id == request.ConversationId && c.CompanyId == companyId)
            .Select(c => new
            {
                c.Id,
                IntegrationType = c.Integration.Type,
                c.Integration.WhatsAppBusinessAccountId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Conversation not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        var dto = new WhatsAppConversationWindowPolicyDto
        {
            ConversationId = conversation.Id,
            IntegrationType = conversation.IntegrationType.ToString(),
            WhatsAppBusinessAccountId = conversation.WhatsAppBusinessAccountId,
            LastInboundAt = null,
            FreeReplyUntil = null,
            RequiresTemplate = false
        };

        if (conversation.IntegrationType != IntegrationType.WhatsApp)
            return new SingleResponse<WhatsAppConversationWindowPolicyDto>(dto);

        var lastInboundAt = await db.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversation.Id && m.Direction == MessageDirection.Inbound)
            .OrderByDescending(m => m.MessageDate)
            .Select(m => (DateTimeOffset?)m.MessageDate)
            .FirstOrDefaultAsync(cancellationToken);

        dto.LastInboundAt = lastInboundAt;

        if (!lastInboundAt.HasValue)
        {
            dto.RequiresTemplate = true;
            return new SingleResponse<WhatsAppConversationWindowPolicyDto>(dto);
        }

        var freeReplyUntil = lastInboundAt.Value.AddHours(24);
        dto.FreeReplyUntil = freeReplyUntil;
        dto.RequiresTemplate = DateTimeOffset.UtcNow > freeReplyUntil;

        return new SingleResponse<WhatsAppConversationWindowPolicyDto>(dto);
    }
}

