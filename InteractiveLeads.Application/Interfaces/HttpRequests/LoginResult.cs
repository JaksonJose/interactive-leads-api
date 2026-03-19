namespace InteractiveLeads.Application.Interfaces.HttpRequests;

public sealed class LoginResult
{
    public string Token { get; set; } = string.Empty;

    public DateTimeOffset? ExpiresAt { get; set; }
}
