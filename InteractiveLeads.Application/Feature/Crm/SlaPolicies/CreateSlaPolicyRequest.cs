namespace InteractiveLeads.Application.Feature.Crm.SlaPolicies;

public sealed class CreateSlaPolicyRequest
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int FirstResponseTargetMinutes { get; set; }
    public int ResolutionTargetMinutes { get; set; }
}
