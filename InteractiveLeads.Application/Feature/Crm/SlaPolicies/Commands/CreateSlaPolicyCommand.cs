using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Application.Feature.Crm.SlaPolicies.Commands;

public sealed class CreateSlaPolicyCommand : IApplicationRequest<IResponse>
{
    public CreateSlaPolicyRequest Body { get; set; } = new();
}

public sealed class CreateSlaPolicyCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : IApplicationRequestHandler<CreateSlaPolicyCommand, IResponse>
{
    public async Task<IResponse> Handle(CreateSlaPolicyCommand request, CancellationToken cancellationToken)
    {
        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var body = request.Body ?? new CreateSlaPolicyRequest();
        var name = (body.Name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
        {
            var bad = new ResultResponse();
            bad.AddErrorMessage("Name is required.", "slaPolicy.validation");
            throw new BadRequestException(bad);
        }

        if (body.FirstResponseTargetMinutes < 1 || body.ResolutionTargetMinutes < 1)
        {
            var bad = new ResultResponse();
            bad.AddErrorMessage("Target minutes must be at least 1.", "slaPolicy.validation");
            throw new BadRequestException(bad);
        }

        var code = NormalizeCode(body.Code);
        if (code is not null)
        {
            var codeTaken = await db.SlaPolicies
                .AnyAsync(p => p.CompanyId == companyId && p.Code == code, cancellationToken);
            if (codeTaken)
            {
                var bad = new ResultResponse();
                bad.AddErrorMessage("Code already in use for this company.", "slaPolicy.codeDuplicate");
                throw new BadRequestException(bad);
            }
        }

        var tenantId = await db.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => c.TenantId)
            .SingleAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var policy = new SlaPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CompanyId = companyId,
            Code = code,
            Name = name,
            Description = NormalizeDescription(body.Description),
            FirstResponseTargetMinutes = body.FirstResponseTargetMinutes,
            ResolutionTargetMinutes = body.ResolutionTargetMinutes,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.SlaPolicies.Add(policy);
        await db.SaveChangesAsync(cancellationToken);

        return new SingleResponse<SlaPolicyDto>(SlaPolicyMapping.ToDto(policy));
    }

    private static string? NormalizeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;
        var t = code.Trim();
        if (t.Length > SlaPolicy.MaxCodeLength)
            return t[..SlaPolicy.MaxCodeLength];
        return t;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;
        var t = description.Trim();
        return t.Length > SlaPolicy.MaxDescriptionLength ? t[..SlaPolicy.MaxDescriptionLength] : t;
    }
}
