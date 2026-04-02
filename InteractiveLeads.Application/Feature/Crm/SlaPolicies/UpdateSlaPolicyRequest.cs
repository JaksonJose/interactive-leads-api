namespace InteractiveLeads.Application.Feature.Crm.SlaPolicies;

public sealed class UpdateSlaPolicyRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? FirstResponseTargetMinutes { get; set; }
    public int? ResolutionTargetMinutes { get; set; }
    public bool? IsActive { get; set; }
}
