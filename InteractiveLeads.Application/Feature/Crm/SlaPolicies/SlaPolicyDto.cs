namespace InteractiveLeads.Application.Feature.Crm.SlaPolicies;

public sealed class SlaPolicyDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int FirstResponseTargetMinutes { get; set; }
    public int ResolutionTargetMinutes { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
