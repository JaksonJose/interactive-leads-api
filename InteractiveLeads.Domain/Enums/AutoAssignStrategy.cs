namespace InteractiveLeads.Domain.Enums;

/// <summary>How the team distributes new conversations among members when auto-assign is enabled.</summary>
public enum AutoAssignStrategy
{
    RoundRobin = 0,
    LeastAssigned = 1,
    Random = 2
}
