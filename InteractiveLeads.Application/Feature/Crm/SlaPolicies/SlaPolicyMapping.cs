using InteractiveLeads.Domain.Entities;

namespace InteractiveLeads.Application.Feature.Crm.SlaPolicies;

internal static class SlaPolicyMapping
{
    public static SlaPolicyDto ToDto(SlaPolicy p) => new()
    {
        Id = p.Id,
        TenantId = p.TenantId,
        CompanyId = p.CompanyId,
        Code = p.Code,
        Name = p.Name,
        Description = p.Description,
        FirstResponseTargetMinutes = p.FirstResponseTargetMinutes,
        ResolutionTargetMinutes = p.ResolutionTargetMinutes,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
