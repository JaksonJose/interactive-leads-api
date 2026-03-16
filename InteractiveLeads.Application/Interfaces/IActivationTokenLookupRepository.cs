using InteractiveLeads.Application.Feature.Activation;

namespace InteractiveLeads.Application.Interfaces
{
    /// <summary>
    /// Global lookup for activation tokens (host DB). Enables resolving token -> TenantId
    /// so the public activation endpoint can use the correct tenant database.
    /// </summary>
    public interface IActivationTokenLookupRepository
    {
        Task<ActivationTokenLookupModel?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
        Task AddAsync(string token, string tenantId, Guid userId, DateTime expiresAt, CancellationToken cancellationToken = default);
        Task MarkAsUsedAsync(string token, CancellationToken cancellationToken = default);
        /// <summary>Marks all non-used lookup entries for the given tenant+user as used.</summary>
        Task InvalidateForUserAsync(string tenantId, Guid userId, CancellationToken cancellationToken = default);
    }
}
