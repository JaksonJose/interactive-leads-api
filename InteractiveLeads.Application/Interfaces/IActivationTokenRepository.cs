using InteractiveLeads.Application.Feature.Activation;

namespace InteractiveLeads.Application.Interfaces
{
    /// <summary>
    /// Repository for user activation tokens.
    /// </summary>
    public interface IActivationTokenRepository
    {
        Task<ActivationTokenModel?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<ActivationTokenModel> AddAsync(Guid userId, string token, DateTime expiresAt, CancellationToken cancellationToken = default);
        Task MarkAsUsedAsync(Guid id, CancellationToken cancellationToken = default);
        /// <summary>
        /// Marks all non-used activation tokens for a user as used/invalid.
        /// </summary>
        Task InvalidateTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
