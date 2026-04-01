namespace InteractiveLeads.Application.Interfaces;

public enum ChatDirectoryMode
{
    /// <summary>All active tenant users with Owner/Manager/Agent roles (collaboration).</summary>
    Participant = 0,
    /// <summary>Active inbox members with Agent role (assign/transfer responsible).</summary>
    Responsible = 1
}

public sealed record ChatDirectoryUserRow(
    string UserId,
    string DisplayName,
    string? Email,
    IReadOnlyList<string> Roles);

/// <summary>Loads tenant users for chat directory UI, optionally scoped to an inbox (responsible mode).</summary>
public interface IChatUserDirectoryService
{
    Task<IReadOnlyList<ChatDirectoryUserRow>> ListAsync(ChatDirectoryMode mode, Guid? inboxId, Guid? teamId, CancellationToken cancellationToken);
}
