using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Crm;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.Queries;

public sealed class GetWhatsAppBusinessAccountQuery : IApplicationRequest<IResponse>
{
    public Guid WhatsAppBusinessAccountId { get; set; }
}

public sealed class GetWhatsAppBusinessAccountQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<GetWhatsAppBusinessAccountQuery, IResponse>
{
    public async Task<IResponse> Handle(GetWhatsAppBusinessAccountQuery request, CancellationToken cancellationToken)
    {
        var companyId = await CrmCompanyResolver.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var waba = await db.WhatsAppBusinessAccounts
            .AsNoTracking()
            .Where(w => w.Id == request.WhatsAppBusinessAccountId && w.CompanyId == companyId)
            .Select(w => new WhatsAppBusinessAccountDto
            {
                Id = w.Id,
                WabaId = w.WabaId,
                Name = w.Name
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (waba == null)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("WhatsApp Business Account not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        return new SingleResponse<WhatsAppBusinessAccountDto>(waba);
    }
}
