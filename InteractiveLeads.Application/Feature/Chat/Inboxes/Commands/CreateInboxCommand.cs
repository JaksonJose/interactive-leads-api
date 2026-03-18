using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Chat.Inboxes.Commands;

public sealed class CreateInboxCommand : IRequest<IResponse>
{
    public CreateInboxRequest CreateInbox { get; set; } = new();
}

public sealed class CreateInboxCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IRequestHandler<CreateInboxCommand, IResponse>
{
    public async Task<IResponse> Handle(CreateInboxCommand request, CancellationToken cancellationToken)
    {
        var tenantIdentifier = currentUserService.GetUserTenant();
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Tenant context is required.", "general.bad_request");
            throw new BadRequestException(response);
        }

        var crmTenantId = await db.Tenants
            .Where(t => t.Identifier == tenantIdentifier)
            .Select(t => t.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (crmTenantId == Guid.Empty)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("CRM tenant not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        var companyId = await db.Companies
            .Where(c => c.TenantId == crmTenantId)
            .Select(c => c.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (companyId == Guid.Empty)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Company not found for current tenant.", "general.not_found");
            throw new NotFoundException(response);
        }

        var companyName = await db.Companies
            .Where(c => c.Id == companyId)
            .Select(c => c.Name)
            .SingleOrDefaultAsync(cancellationToken) ?? string.Empty;

        var name = (request.CreateInbox?.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Inbox name is required.", "chat.inbox.name_required");
            throw new BadRequestException(response);
        }

        if (name.Length > 150)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Inbox name is too long.", "chat.inbox.name_too_long");
            throw new BadRequestException(response);
        }

        var inbox = new Inbox
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = name,
            IsActive = request.CreateInbox?.IsActive ?? true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Inboxes.Add(inbox);
        await db.SaveChangesAsync(cancellationToken);

        return new SingleResponse<InboxDto>(new InboxDto
        {
            Id = inbox.Id,
            Name = inbox.Name,
            CompanyName = companyName,
            IsActive = inbox.IsActive,
            CreatedAt = inbox.CreatedAt
        });
    }
}

