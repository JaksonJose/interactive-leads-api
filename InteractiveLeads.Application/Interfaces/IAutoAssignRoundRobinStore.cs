namespace InteractiveLeads.Application.Interfaces;

/// <summary>Atomic round-robin slot selection per team (Redis-backed in production).</summary>
public interface IAutoAssignRoundRobinStore
{
    /// <returns>Index in <paramref name="candidateCount"/> (0-based).</returns>
    Task<int> GetNextSlotIndexAsync(Guid teamId, int candidateCount, CancellationToken cancellationToken);
}
