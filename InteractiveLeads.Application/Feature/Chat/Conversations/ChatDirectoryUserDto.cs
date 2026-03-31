namespace InteractiveLeads.Application.Feature.Chat.Conversations;

public sealed class ChatDirectoryUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public List<string> Roles { get; set; } = [];
    public bool IsOnline { get; set; }
    public DateTimeOffset? LastSeenAtUtc { get; set; }
}
