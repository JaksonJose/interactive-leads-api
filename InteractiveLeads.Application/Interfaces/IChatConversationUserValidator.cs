namespace InteractiveLeads.Application.Interfaces;

/// <summary>
/// Validates users for assign/transfer (must be Agent + inbox member) and for participant invites (active tenant user with chat roles).
/// </summary>
public interface IChatConversationUserValidator
{
    /// <exception cref="Exceptions.BadRequestException">When validation fails.</exception>
    Task EnsureValidResponsibleTargetAsync(Guid targetUserId, Guid inboxId, CancellationToken cancellationToken);

    /// <exception cref="Exceptions.BadRequestException">When validation fails.</exception>
    Task EnsureValidParticipantTargetAsync(Guid targetUserId, CancellationToken cancellationToken);
}
