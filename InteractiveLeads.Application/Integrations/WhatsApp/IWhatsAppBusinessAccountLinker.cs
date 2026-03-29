namespace InteractiveLeads.Application.Integrations.WhatsApp;

/// <summary>Creates or resolves WABA rows (<see cref="InteractiveLeads.Domain.Entities.WhatsAppBusinessAccount"/>) from Meta ids in integration settings.</summary>
public interface IWhatsAppBusinessAccountLinker
{
    /// <summary>Returns persisted WABA primary key, or <c>null</c> when <paramref name="metaBusinessAccountId"/> is empty.</summary>
    Task<Guid?> EnsureWabaIdAsync(Guid companyId, string? metaBusinessAccountId, CancellationToken cancellationToken);
}
