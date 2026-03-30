using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Crm;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.Commands;

public sealed class CreateWhatsAppBusinessAccountCommand : IApplicationRequest<IResponse>
{
    public CreateWhatsAppBusinessAccountRequest Account { get; set; } = new();
}

public sealed class CreateWhatsAppBusinessAccountCommandHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    : IApplicationRequestHandler<CreateWhatsAppBusinessAccountCommand, IResponse>
{
    public async Task<IResponse> Handle(CreateWhatsAppBusinessAccountCommand request, CancellationToken cancellationToken)
    {
        var companyId = await CrmCompanyResolver.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var wabaId = (request.Account.WabaId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(wabaId) || wabaId.Length > 128)
        {
            var bad = new ResultResponse();
            bad.AddErrorMessage("WhatsApp Business Account id is invalid.", "integrations.whatsapp.business_account_id_required");
            throw new BadRequestException(bad);
        }

        var name = string.IsNullOrWhiteSpace(request.Account.Name) ? null : request.Account.Name!.Trim();
        if (name is { Length: > 256 })
        {
            var bad = new ResultResponse();
            bad.AddErrorMessage("Display name is too long.", "integrations.waba_name_too_long");
            throw new BadRequestException(bad);
        }

        var exists = await db.WhatsAppBusinessAccounts
            .AnyAsync(w => w.CompanyId == companyId && w.WabaId == wabaId, cancellationToken);

        if (exists)
        {
            var dup = new ResultResponse();
            dup.AddErrorMessage("This WhatsApp Business Account is already registered.", "integrations.waba_duplicate");
            throw new BadRequestException(dup);
        }

        var entity = new WhatsAppBusinessAccount
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            WabaId = wabaId,
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.WhatsAppBusinessAccounts.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        var dto = new WhatsAppBusinessAccountDto
        {
            Id = entity.Id,
            WabaId = entity.WabaId,
            Name = entity.Name
        };

        return new SingleResponse<WhatsAppBusinessAccountDto>(dto);
    }
}
