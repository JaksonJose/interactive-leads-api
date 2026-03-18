namespace InteractiveLeads.Application.Interfaces;

/// <summary>
/// Looks up basic user information (display name / email) by user id within the current tenant scope.
/// </summary>
public interface IUserSummaryLookupService
{
    /// <summary>
    /// Returns a map keyed by userId (string) with display name and email.
    /// </summary>
    Task<IReadOnlyDictionary<string, (string? DisplayName, string? Email)>> GetSummariesByIdsAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken ct = default);
}

