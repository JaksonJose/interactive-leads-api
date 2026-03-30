namespace InteractiveLeads.Application.Realtime.Models;

public sealed class MessageStatusUpdatedPayloadDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }

    /// <summary>Lowercase: pending, sent, delivered, read, failed.</summary>
    public string Status { get; set; } = "pending";

    /// <summary>Optional detail when status is <c>failed</c> (outbound).</summary>
    public string? FailureMessage { get; set; }
}
