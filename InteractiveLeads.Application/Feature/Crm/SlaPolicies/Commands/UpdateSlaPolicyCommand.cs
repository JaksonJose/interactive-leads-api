using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.SlaPolicies.Commands;

public sealed class UpdateSlaPolicyCommand : IApplicationRequest<IResponse>
{
    public Guid PolicyId { get; set; }
    public UpdateSlaPolicyRequest Body { get; set; } = new();
}

public sealed class UpdateSlaPolicyCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<UpdateSlaPolicyCommand, IResponse>
{
    public async Task<IResponse> Handle(UpdateSlaPolicyCommand request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var policy = await db.SlaPolicies
            .FirstOrDefaultAsync(p => p.Id == request.PolicyId && p.CompanyId == companyId, cancellationToken);

        if (policy is null)
        {
            var nf = new ResultResponse();
            nf.AddErrorMessage("SLA policy not found.", "general.not_found");
            throw new NotFoundException(nf);
        }

        var body = request.Body ?? new UpdateSlaPolicyRequest();

        if (body.Name is not null)
        {
            var name = body.Name.Trim();
            if (string.IsNullOrEmpty(name))
            {
                var bad = new ResultResponse();
                bad.AddErrorMessage("Name cannot be empty.", "slaPolicy.validation");
                throw new BadRequestException(bad);
            }

            policy.Name = name.Length > SlaPolicy.MaxNameLength ? name[..SlaPolicy.MaxNameLength] : name;
        }

        if (body.Code is not null)
        {
            var code = string.IsNullOrWhiteSpace(body.Code) ? null : body.Code.Trim();
            if (code is not null && code.Length > SlaPolicy.MaxCodeLength)
                code = code[..SlaPolicy.MaxCodeLength];

            if (code != policy.Code)
            {
                var taken = await db.SlaPolicies
                    .AnyAsync(
                        p => p.CompanyId == companyId && p.Code == code && p.Id != policy.Id,
                        cancellationToken);
                if (taken)
                {
                    var bad = new ResultResponse();
                    bad.AddErrorMessage("Code already in use for this company.", "slaPolicy.codeDuplicate");
                    throw new BadRequestException(bad);
                }
            }

            policy.Code = code;
        }

        if (body.Description is not null)
            policy.Description = NormalizeDescription(body.Description);

        if (body.FirstResponseTargetMinutes.HasValue)
        {
            if (body.FirstResponseTargetMinutes.Value < 1)
            {
                var bad = new ResultResponse();
                bad.AddErrorMessage("First response target minutes must be at least 1.", "slaPolicy.validation");
                throw new BadRequestException(bad);
            }

            policy.FirstResponseTargetMinutes = body.FirstResponseTargetMinutes.Value;
        }

        if (body.ResolutionTargetMinutes.HasValue)
        {
            if (body.ResolutionTargetMinutes.Value < 1)
            {
                var bad = new ResultResponse();
                bad.AddErrorMessage("Resolution target minutes must be at least 1.", "slaPolicy.validation");
                throw new BadRequestException(bad);
            }

            policy.ResolutionTargetMinutes = body.ResolutionTargetMinutes.Value;
        }

        if (body.IsActive.HasValue)
            policy.IsActive = body.IsActive.Value;

        policy.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return new SingleResponse<SlaPolicyDto>(SlaPolicyMapping.ToDto(policy));
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;
        var t = description.Trim();
        return t.Length > SlaPolicy.MaxDescriptionLength ? t[..SlaPolicy.MaxDescriptionLength] : t;
    }
}
