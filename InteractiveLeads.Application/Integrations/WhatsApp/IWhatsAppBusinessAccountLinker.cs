namespace InteractiveLeads.Application.Integrations.WhatsApp;

/// <summary>Resolves CRM WABA rows (<see cref="InteractiveLeads.Domain.Entities.WhatsAppBusinessAccount"/>) from Meta ids; accounts must be registered first.</summary>
public interface IWhatsAppBusinessAccountLinker
{
    /// <summary>Returns persisted WABA primary key, or <c>null</c> when <paramref name="metaBusinessAccountId"/> is empty or not registered for the company.</summary>
    Task<Guid?> EnsureWabaIdAsync(Guid companyId, string? metaBusinessAccountId, CancellationToken cancellationToken);
}
